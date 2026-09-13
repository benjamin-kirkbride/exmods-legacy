// On the legacy target frameworks, bring the LegacyApi extension members into scope across
// the whole assembly so consumers (ExRecipeCosts, ExParticles, the multiblock BE) keep using
// the 1.22 member names unchanged. On the current version this is absent and the real game members are used.
#if !GAME_GE_1_22
global using ExpandedLib.Legacy;
#endif
