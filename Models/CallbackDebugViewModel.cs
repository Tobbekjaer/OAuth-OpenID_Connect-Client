namespace OAuth_OpenID_Connect_Client.Models;

public class CallbackDebugViewModel
{
    public string? Code { get; set; }
    public string? ReturnedState { get; set; }
    public string? ExpectedState { get; set; }
    public bool StateMatches { get; set; }
}