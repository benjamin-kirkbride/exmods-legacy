#!/usr/bin/env python3
"""Writes one ReleasedHistory golden per published release zip.

Usage: released-goldens.py ZIP_DIR GAME_ASSETS OUT_DIR

Reads every zip in ZIP_DIR by its modinfo's modid and version and writes OUT_DIR/<modid>-<version>.json.
Blocktypes and itemtypes are expanded by the game's variant rules: variantgroups in order (states or
loadFromProperties, combine multiply), then allowedVariants and skipVariants as wildcard patterns over
the code path.

The release's patches are applied as ModJsonPatchLoader applies them on a server: patch files in path
order and patches in array order; enabled false and side Client skipped; every condition unmet (there
is no world config); dependsOn met by the mods of the load, invert honoured. The load is
GAME_ASSETS/survival as domain game, the release, and the newest published ppex for a release whose
modinfo depends on ppex (recorded as pairedWith). A patch whose target file or path is missing applies
nothing and is written to stderr with its patch file and index.

Rows are the release's own files as patched, then one row per file of another domain that the patches
changed, holding only the codes the patched file has and the unpatched file lacks. Game files are read
as JSON5: unquoted keys, single quotes, comments, trailing commas.
"""
import copy, itertools, json, pathlib, re, sys, zipfile

GAME = 'game'
SPACE = re.compile(r'[\s\ufeff]+')
NUMBER = re.compile(r'[+-]?(?:0[xX][0-9a-fA-F]+|Infinity|NaN|(?:\d+\.?\d*|\.\d+)(?:[eE][+-]?\d+)?)')
IDENT = re.compile(r'[^\W\d][\w$]*|\$[\w$]*')
ESCAPES = {'n': '\n', 't': '\t', 'r': '\r', 'b': '\b', 'f': '\f', 'v': '\v', '0': '\0'}


class Json5Error(ValueError):
    pass


class Json5:
    def __init__(self, text):
        self.s = text.lstrip('\ufeff')
        self.i = 0

    def fail(self, what):
        raise Json5Error(f'{what} at line {self.s.count(chr(10), 0, self.i) + 1}')

    def ws(self):
        s = self.s
        while True:
            m = SPACE.match(s, self.i)
            if m:
                self.i = m.end()
            if s.startswith('//', self.i):
                end = s.find('\n', self.i)
                self.i = len(s) if end < 0 else end
            elif s.startswith('/*', self.i):
                end = s.find('*/', self.i + 2)
                if end < 0:
                    self.fail('unterminated comment')
                self.i = end + 2
            elif not m:
                return

    def parse(self):
        v = self.value()
        self.ws()
        if self.i != len(self.s):
            self.fail('trailing content')
        return v

    def value(self):
        self.ws()
        if self.i >= len(self.s):
            self.fail('unexpected end')
        c = self.s[self.i]
        if c == '{':
            return self.obj()
        if c == '[':
            return self.arr()
        if c in '"\'':
            return self.string()
        m = NUMBER.match(self.s, self.i)
        if m:
            self.i = m.end()
            t = m.group().lstrip('+-')
            if t.lower().startswith('0x'):
                return int(m.group(), 16)
            if t in ('Infinity', 'NaN'):
                return float(m.group().replace('Infinity', 'inf').replace('NaN', 'nan'))
            return float(m.group()) if re.search(r'[.eE]', t) else int(m.group())
        for word, val in (('true', True), ('false', False), ('null', None)):
            if self.s.startswith(word, self.i):
                self.i += len(word)
                return val
        self.fail(f'unexpected {c!r}')

    def obj(self):
        self.i += 1
        out = {}
        while True:
            self.ws()
            if self.i >= len(self.s):
                self.fail('unterminated object')
            if self.s[self.i] == '}':
                self.i += 1
                return out
            if self.s[self.i] in '"\'':
                key = self.string()
            else:
                m = IDENT.match(self.s, self.i)
                if not m:
                    self.fail('bad property name')
                key = m.group()
                self.i = m.end()
            self.ws()
            if self.i >= len(self.s) or self.s[self.i] != ':':
                self.fail('expected :')
            self.i += 1
            out[key] = self.value()
            self.ws()
            if self.s.startswith(',', self.i):
                self.i += 1
            elif not self.s.startswith('}', self.i):
                self.fail('expected , or }')

    def arr(self):
        self.i += 1
        out = []
        while True:
            self.ws()
            if self.i >= len(self.s):
                self.fail('unterminated array')
            if self.s[self.i] == ']':
                self.i += 1
                return out
            out.append(self.value())
            self.ws()
            if self.s.startswith(',', self.i):
                self.i += 1
            elif not self.s.startswith(']', self.i):
                self.fail('expected , or ]')

    def string(self):
        quote = self.s[self.i]
        self.i += 1
        out = []
        while True:
            if self.i >= len(self.s):
                self.fail('unterminated string')
            c = self.s[self.i]
            self.i += 1
            if c == quote:
                return ''.join(out)
            if c != '\\':
                out.append(c)
                continue
            c = self.s[self.i]
            self.i += 1
            if c in ESCAPES:
                out.append(ESCAPES[c])
            elif c == 'x':
                out.append(chr(int(self.s[self.i:self.i + 2], 16)))
                self.i += 2
            elif c == 'u':
                out.append(chr(int(self.s[self.i:self.i + 4], 16)))
                self.i += 4
            elif c == '\r':
                if self.s.startswith('\n', self.i):
                    self.i += 1
            elif c not in '\n\u2028\u2029':
                out.append(c)


def json5(text):
    return Json5(text).parse()


def field(d, name, default=None):
    for k, v in d.items():
        if k.lower() == name.lower():
            return v
    return default


def version_key(v):
    return tuple(int(x) for x in re.match(r'(\d+)\.(\d+)\.(\d+)', v).groups())


class Zip:
    def __init__(self, path):
        z = zipfile.ZipFile(path)
        self.files = {n: z.read(n) for n in z.namelist() if not n.endswith('/')}
        info = json5(self.files['modinfo.json'].decode('utf-8-sig'))
        self.domain = field(info, 'modid')
        self.version = field(info, 'version')
        self.depends = {k.lower() for k in (field(info, 'dependencies') or {})}

    def get(self, domain, path):
        data = self.files.get(f'assets/{domain}/{path}')
        return None if data is None else data.decode('utf-8-sig')

    def paths(self, domain, prefix):
        head = f'assets/{domain}/'
        return sorted(n[len(head):] for n in self.files if n.startswith(head + prefix))

    def domains(self):
        return {n.split('/')[1] for n in self.files if n.startswith('assets/') and n.count('/') >= 2}


class GameAssets:
    domain = GAME
    version = None

    def __init__(self, assets):
        self.root = pathlib.Path(assets) / 'survival'

    def get(self, domain, path):
        if domain != GAME:
            return None
        f = self.root / path
        return f.read_text(encoding='utf-8-sig') if f.is_file() else None

    def paths(self, domain, prefix):
        if domain != GAME:
            return []
        return sorted(
            f.relative_to(self.root).as_posix() for f in (self.root / prefix).rglob('*') if f.is_file()
        )

    def domains(self):
        return {GAME}


class PatchError(Exception):
    pass


MISSING = object()
INT = re.compile(r'[+-]?\d+')


def pointer(text):
    return [
        re.sub(r'%([0-9A-Fa-f]{2})', lambda m: chr(int(m.group(1), 16)), t).replace('~1', '/').replace('~0', '~')
        for t in text.split('/')[1:]
    ]


def step(node, token):
    if isinstance(node, list):
        if INT.fullmatch(token):
            i = int(token)
            return node[i] if -len(node) <= i < len(node) else MISSING
        return MISSING
    if isinstance(node, dict):
        return node.get(token, MISSING)
    return MISSING


def find(doc, tokens, skip_last=False):
    node = doc
    for depth in range(len(tokens) - (1 if skip_last else 0)):
        node = step(node, tokens[depth])
        if node is MISSING:
            raise PatchError(f'path /{"/".join(tokens)} not found at {tokens[depth]}')
    return node


def json_type(v):
    return 'object' if isinstance(v, dict) else 'array' if isinstance(v, list) else 'value'


def merge_into(target, source):
    if isinstance(target, dict):
        if not isinstance(source, dict):
            return
        for k, v in source.items():
            if k not in target:
                target[k] = copy.deepcopy(v)
            elif v is not None:
                if json_type(target[k]) == json_type(v) != 'value':
                    merge_into(target[k], v)
                else:
                    target[k] = copy.deepcopy(v)
    else:
        if not isinstance(source, list):
            raise PatchError('array merge needs an array value')
        target.extend(copy.deepcopy(source))


def add(doc, tokens, value, merge):
    parent = find(doc, tokens, True)
    last = tokens[-1]
    if INT.fullmatch(last):
        if not isinstance(parent, list) or not 0 <= int(last) <= len(parent):
            raise PatchError(f'cannot insert at {last}')
        parent.insert(int(last), copy.deepcopy(value))
    elif last == '-':
        if not isinstance(parent, list):
            raise PatchError('cannot append to a non-array')
        parent.append(copy.deepcopy(value))
    elif not isinstance(parent, dict):
        raise PatchError(f'cannot set {last} on a non-object')
    elif merge and isinstance(parent.get(last), (dict, list)):
        merge_into(parent[last], value)
    else:
        parent[last] = copy.deepcopy(value)


def remove(doc, tokens):
    if not tokens:
        raise PatchError('cannot remove the root')
    parent = find(doc, tokens, True)
    last = tokens[-1]
    if isinstance(parent, list) and INT.fullmatch(last) and -len(parent) <= int(last) < len(parent):
        del parent[int(last)]
    elif isinstance(parent, dict) and last in parent:
        del parent[last]
    else:
        raise PatchError(f'path /{"/".join(tokens)} not found at {last}')


def apply_op(doc, patch):
    op = str(patch['op']).lower()
    tokens = pointer(patch['path'])
    value = patch.get('value')
    if op in ('add', 'addmerge', 'addeach', 'replace') and value is None:
        raise PatchError(f'{op} needs a value')
    if op == 'add':
        add(doc, tokens, value, False)
    elif op == 'addmerge':
        add(doc, tokens, value, True)
    elif op == 'addeach':
        parent = find(doc, tokens, True)
        last = tokens[-1]
        if not isinstance(value, list) or not isinstance(parent, list):
            raise PatchError('addeach needs an array value and an array target')
        if last == '-':
            parent.extend(copy.deepcopy(value))
        elif INT.fullmatch(last) and 0 <= int(last) <= len(parent):
            parent[int(last):int(last)] = copy.deepcopy(value)
        else:
            raise PatchError('addeach path must be an index or -')
    elif op == 'remove':
        remove(doc, tokens)
    elif op == 'replace':
        find(doc, tokens)
        parent = find(doc, tokens, True)
        parent[int(tokens[-1]) if isinstance(parent, list) else tokens[-1]] = copy.deepcopy(value)
    elif op in ('copy', 'move'):
        src = pointer(patch['frompath'] if 'frompath' in patch else patch['from'])
        if op == 'move' and patch['path'].startswith('/' + '/'.join(src)):
            raise PatchError('to path cannot be below from path')
        add(doc, tokens, copy.deepcopy(find(doc, src)), True)
        if op == 'move':
            remove(doc, src)
    else:
        raise PatchError(f'unknown op {op}')


def with_domain(loc):
    domain, _, path = loc.rpartition(':')
    return (domain or GAME), path if path.endswith('.json') else path + '.json'


def patches_of(release, mod_ids):
    """Yields (source, index, patch) for the patches of `release` that a server applies."""
    for path in [p for p in release.paths(release.domain, 'patches/') if p.endswith('.json')]:
        try:
            patches = json5(release.get(release.domain, path))
        except Json5Error as e:
            print(f'{release.domain}-{release.version} {path}: unreadable: {e}', file=sys.stderr)
            continue
        for j, raw in enumerate(patches):
            patch = {k.lower(): v for k, v in raw.items()}
            if patch.get('enabled', True) is False:
                continue
            if str(patch.get('side') or '').lower() == 'client':
                continue
            if patch.get('condition') is not None:
                continue
            deps = patch.get('dependson')
            if deps is not None and not all(
                (field(d, 'modid') in mod_ids) != bool(field(d, 'invert', False)) for d in deps
            ):
                continue
            yield f'{release.domain}:{path}', j, patch


def read(load, domain, path):
    return next((t for m in load if (t := m.get(domain, path)) is not None), None)


def apply_patches(load, release, mod_ids):
    """Returns ({(domain, path): patched doc}, unapplied count); unapplied patches go to stderr."""
    docs, unapplied = {}, 0
    for source, index, patch in patches_of(release, mod_ids):
        domain, path = with_domain(patch['file'])
        targets = [(domain, path)]
        if patch['file'].endswith('*'):
            prefix = with_domain(patch['file'][:-1])[1][: -len('.json')]
            targets = [(domain, p) for m in load for p in m.paths(domain, prefix)]
        for target in targets:
            try:
                if target not in docs:
                    text = read(load, *target)
                    if text is None:
                        raise PatchError(f'file {target[0]}:{target[1]} not found')
                    docs[target] = json5(text)
                apply_op(docs[target], patch)
            except (PatchError, Json5Error, TypeError, KeyError, ValueError) as e:
                unapplied += 1
                print(f'{release.domain}-{release.version} {source} #{index}: {e}', file=sys.stderr)
    return docs, unapplied


def matches(pattern, path):
    pattern = pattern.split(':', 1)[-1]
    if pattern.startswith('@'):
        return re.fullmatch(pattern[1:], path) is not None
    return re.fullmatch('.*'.join(map(re.escape, pattern.split('*'))), path) is not None


def expand(doc, domain, load):
    def props(ref):
        dom, _, path = ref.rpartition(':')
        text = read(load, dom or GAME, f'worldproperties/{path}.json')
        if text is None:
            raise SystemExit(f'{domain}:{field(doc, "code")}: worldproperties {ref} not found')
        return [field(v, 'code') for v in field(json5(text), 'variants')]

    code = field(doc, 'code')
    groups = []
    for g in field(doc, 'variantgroups', []):
        if str(field(g, 'combine', 'multiply')).lower() != 'multiply':
            raise SystemExit(f'{domain}:{code}: combine {field(g, "combine")} not handled')
        states = field(g, 'states')
        groups.append(states if states is not None else props(field(g, 'loadFromProperties')))
    paths = ['-'.join([code, *combo]) for combo in itertools.product(*groups)]
    allowed, skip = field(doc, 'allowedVariants'), field(doc, 'skipVariants')
    if allowed is not None:
        paths = [p for p in paths if any(matches(a, p) for a in allowed)]
    if skip is not None:
        paths = [p for p in paths if not any(matches(s, p) for s in skip)]
    return sorted(f'{domain}:{p}' for p in paths)


def row(domain, rel, doc, codes):
    return {'domain': domain, 'assetPath': rel, 'baseCode': f'{domain}:{field(doc, "code")}', 'codes': codes}


def build(release, load, mod_ids):
    docs, unapplied = apply_patches(load, release, mod_ids)
    own = {'blocktypes': [], 'itemtypes': []}
    classes = {}
    for kind in own:
        files = [p for p in release.paths(release.domain, f'{kind}/') if p.endswith('.json')]
        for path in sorted(files, key=lambda p: p.split('/')):
            doc = docs.get((release.domain, path))
            if doc is None:
                doc = json5(release.get(release.domain, path))
            if field(doc, 'enabled') is False:
                continue
            rel = path[len(kind) + 1: -len('.json')]
            own[kind].append(row(release.domain, rel, doc, expand(doc, release.domain, load)))
            if kind != 'blocktypes':
                continue
            for key, value in doc.items():
                if key.lower() == 'entityclass':
                    classes.setdefault(value, []).append(rel)
                elif key.lower() == 'entityclassbytype':
                    for v in value.values():
                        classes.setdefault(v, []).append(rel)
    extra = {'blocktypes': [], 'itemtypes': []}
    for (domain, path), doc in sorted(docs.items()):
        kind = path.split('/', 1)[0]
        if domain == release.domain or kind not in extra:
            continue
        before = json5(read(load, domain, path))
        gained = sorted(set(expand(doc, domain, load)) - set(expand(before, domain, load)))
        if gained:
            extra[kind].append(row(domain, path[len(kind) + 1: -len('.json')], doc, gained))
    return own, extra, classes, unapplied


def main():
    zip_dir, game_assets, out = sys.argv[1:4]
    out = pathlib.Path(out)
    out.mkdir(parents=True, exist_ok=True)
    game = GameAssets(game_assets)

    parsed = failed = 0
    for kind in ('blocktypes', 'itemtypes'):
        for path in game.paths(GAME, f'{kind}/'):
            if not path.endswith('.json'):
                continue
            try:
                json5(game.get(GAME, path))
                parsed += 1
            except (Json5Error, ValueError) as e:
                failed += 1
                print(f'{path}: {e}', file=sys.stderr)
    print(f'game blocktypes and itemtypes read as JSON5: {parsed} parsed, {failed} failures', file=sys.stderr)

    zips = sorted(
        (Zip(p) for p in pathlib.Path(zip_dir).glob('*.zip')), key=lambda z: (z.domain, version_key(z.version))
    )
    newest_ppex = max((z for z in zips if z.domain == 'ppex'), key=lambda z: version_key(z.version), default=None)
    for release in zips:
        paired = release.domain != 'ppex' and 'ppex' in release.depends and newest_ppex is not None
        load = [game, newest_ppex, release] if paired else [game, release]
        mod_ids = {'game', 'survival', 'creative', 'essentials'} | {m.domain for m in load}
        own, extra, classes, unapplied = build(release, load, mod_ids)
        doc = {
            'mod': release.domain,
            'version': release.version,
            'shipped': own['blocktypes'] + extra['blocktypes'],
            'entityClasses': [
                {'domain': release.domain, 'class': c, 'assetPaths': sorted(set(p))}
                for c, p in sorted(classes.items())
            ],
            'items': own['itemtypes'] + extra['itemtypes'],
        }
        if paired:
            doc['pairedWith'] = {'ppex': newest_ppex.version}
        (out / f'{release.domain}-{release.version}.json').write_text(
            json.dumps(doc, indent=2) + '\n', encoding='ascii'
        )
        count = lambda rows: sum(len(r['codes']) for r in rows)
        print(
            f'{release.domain} {release.version}: {len(own["blocktypes"])} rows, {count(own["blocktypes"])} codes, '
            f'{count(own["itemtypes"])} item codes, {len(extra["blocktypes"])} patch rows with '
            f'{count(extra["blocktypes"])} codes, {len(extra["itemtypes"])} patch item rows with '
            f'{count(extra["itemtypes"])} codes, {unapplied} unapplied patches, {len(classes)} entity classes'
        )


main()
