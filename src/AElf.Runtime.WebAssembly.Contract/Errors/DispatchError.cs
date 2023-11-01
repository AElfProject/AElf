namespace AElf.Runtime.WebAssembly.Contract;

public enum DispatchError
{
    /// Some error occurred.
    Other,

    /// Failed to lookup some data.
    CannotLookup,

    /// A bad origin.
    BadOrigin,

    /// A custom error in a module.
    Module,

    /// At least one consumer is remaining so the account cannot be destroyed.
    ConsumerRemaining,

    /// There are no providers so the account cannot be created.
    NoProviders,

    /// There are too many consumers so the account cannot be created.
    TooManyConsumers,

    /// An error to do with tokens.
    Token,

    /// An arithmetic error.
    Arithmetic,

    /// The number of transactional layers has been reached, or we are not in a transactional
    /// layer.
    Transactional,

    /// Resources exhausted, e.g. attempt to read/write data which is too large to manipulate.
    Exhausted,

    /// The state is corrupt; this is generally not going to fix itself.
    Corruption,

    /// Some resource (e.g. a preimage) is unavailable right now. This might fix itself later.
    Unavailable,

    /// Root origin is not allowed.
    RootNotAllowed,
}