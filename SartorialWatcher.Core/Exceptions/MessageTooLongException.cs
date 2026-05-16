namespace SartorialWatcher.Core.Exceptions;

public class MessageTooLongException(int limit, string? message = null) : Exception(message);