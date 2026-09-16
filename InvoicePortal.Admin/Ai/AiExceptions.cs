namespace InvoicePortal.Admin.Ai;

/// <summary>Base for every user-facing AI failure; pages catch this one type and show <see cref="Exception.Message"/>.</summary>
public class AiException(string message) : Exception(message);

/// <summary>The request or the generated SQL was rejected by our own validation (not by the model).</summary>
public sealed class QueryRejectedException(string message) : AiException(message);

/// <summary>The model returned something that does not honour the JSON / citation contract.</summary>
public sealed class ModelResponseException(string message) : AiException(message);

public sealed class AiRateLimitException() : AiException("Too many AI requests. Try again in a minute.");
