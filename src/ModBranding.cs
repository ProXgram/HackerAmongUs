namespace HAU;

public static class ModBranding
{
    public const string Name = "Hacker Among Us";
    public const string ShortName = "HAU";
    public const string Creator = "ProXgram";

#if JUDGE_ROLE_EXPERIMENT
    public const string Version = "1.0.3-judge-test";
#elif GUEST_KILL_EXPERIMENT
    public const string Version = "1.0.3-guest-test";
#else
    public const string Version = "1.0.3";
#endif
}
