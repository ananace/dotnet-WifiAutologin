namespace WifiAutologin;

public interface ILogger
{
    void Debug(string message);
    void Info(string message);
    void Warn(string message);
    void Error(string message);
    void Critical(string message);

    void Debug(string message, params object[] formatObjs);
    void Info(string message, params object[] formatObjs);
    void Warn(string message, params object[] formatObjs);
    void Error(string message, params object[] formatObjs);
    void Critical(string message, params object[] formatObjs);
}
