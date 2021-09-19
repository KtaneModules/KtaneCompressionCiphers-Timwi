using CipherModulesLib;

public class LempelZivCipherModule : CipherModuleBase
{
    private static int _moduleIdCounter = 1;
    private int _moduleId;
    protected override string loggingTag => $"Lempel-Ziv Cipher #{_moduleId}";

    void Start()
    {
        _moduleId = _moduleIdCounter++;
        _answer = "SOLVE";
    }

    protected override int getFontSize(int page, int screen) => 35;
}