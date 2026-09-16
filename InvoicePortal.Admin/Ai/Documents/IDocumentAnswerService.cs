namespace InvoicePortal.Admin.Ai.Documents;

public interface IDocumentAnswerService
{
    Task<DocumentAnswer> AskAsync(string question, CancellationToken cancellationToken = default);
    Task<string> DefaultQuestionAsync(CancellationToken cancellationToken = default);
}
