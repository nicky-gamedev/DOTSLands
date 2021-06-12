using System;

namespace libuv2k.Native
{
    public sealed class OperationException : Exception
    {
        public readonly int Code;
        public readonly string Name;
        public readonly ErrorCode ErrorCode;

        public OperationException(int errorCode, string errorName, string description)
            : base($"{errorName} : {description}")
        {
            Code = errorCode;
            Name = errorName;

            if (!Enum.TryParse(errorName, out ErrorCode value))
            {
                value = ErrorCode.UNKNOWN;
            }

            ErrorCode = value;
        }

        public override string Message => $"{Name} ({ErrorCode}) : {base.Message}";
    }

    // ReSharper disable InconsistentNaming
    public enum ErrorCode
    {
        E2BIG,
        EACCES,
        EADDRINUSE,
        EADDRNOTAVAIL,
        EAFNOSUPPORT,
        EAGAIN,
        EAI_ADDRFAMILY,
        EAI_AGAIN,
        EAI_BADFLAGS,
        EAI_BADHINTS,
        EAI_CANCELED,
        EAI_FAIL,
        EAI_FAMILY,
        EAI_MEMORY,
        EAI_NODATA,
        EAI_NONAME,
        EAI_OVERFLOW,
        EAI_PROTOCOL,
        EAI_SERVICE,
        EAI_SOCKTYPE,
        EALREADY,
        EBADF,
        EBUSY,
        ECANCELED,
        ECHARSET,
        ECONNABORTED,
        ECONNREFUSED,
        ECONNRESET,
        EDESTADDRREQ,
        EEXIST,
        EFAULT,
        EFBIG,
        EHOSTUNREACH,
        EINTR,
        EINVAL,
        EIO,
        EISCONN,
        EISDIR,
        ELOOP,
        EMFILE,
        EMSGSIZE,
        ENAMETOOLONG,
        ENETDOWN,
        ENETUNREACH,
        ENFILE,
        ENOBUFS,
        ENODEV,
        ENOENT,
        ENOMEM,
        ENONET,
        ENOPROTOOPT,
        ENOSPC,
        ENOSYS,
        ENOTCONN,
        ENOTDIR,
        ENOTEMPTY,
        ENOTSOCK,
        ENOTSUP,
        EPERM,
        EPIPE,
        EPROTO,
        EPROTONOSUPPORT,
        EPROTOTYPE,
        ERANGE,
        EROFS,
        ESHUTDOWN,
        ESPIPE,
        ESRCH,
        ETIMEDOUT,
        ETXTBSY,
        EXDEV,
        UNKNOWN,
        EOF,
        ENXIO,
        EMLINK,
    }
}
