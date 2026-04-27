namespace simulationTest.Interfaces;

public interface IEmailService
{
    Task SendAsync(string subject, string body);
}
