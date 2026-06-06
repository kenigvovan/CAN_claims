using System;

namespace claims.src.delayed
{
    public interface IExpirable
    {
        string Guid { get; }
        long TimeStampExpire { get; }
        Action OnExpire { get; }
    }
}
