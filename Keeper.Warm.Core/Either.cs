using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Keeper.Warm
{
    public abstract class Either<TLeft, TRight>
    {
        public static implicit operator Either<TLeft, TRight>(TLeft value)
        {
            return new Left(value);
        }

        public static implicit operator Either<TLeft, TRight>(TRight value)
        {
            return new Right(value);
        }

        public static explicit operator TLeft(Either<TLeft, TRight> value)
        {
            var left = value as Left;

            if (left == null)
            {
                throw new InvalidCastException();
            }
            else
            {
                return left.Value;
            }
        }

        public static explicit operator TRight(Either<TLeft, TRight> value)
        {
            var Right = value as Right;

            if (Right == null)
            {
                throw new InvalidCastException();
            }
            else
            {
                return Right.Value;
            }
        }

        public virtual bool IsLeft
        {
            get
            {
                return false;
            }
        }

        public virtual bool IsRight
        {
            get
            {
                return false;
            }
        }

        private sealed class Left
            : Either<TLeft, TRight>
        {
            public Left(TLeft value)
            {
                this.Value = value;
            }

            public TLeft Value
            {
                get;
                private set;
            }

            public override bool IsLeft
            {
                get
                {
                    return true;
                }
            }
        }

        private sealed class Right
            : Either<TLeft, TRight>
        {
            public Right(TRight value)
            {
                this.Value = value;
            }

            public TRight Value
            {
                get;
                private set;
            }

            public override bool IsRight
            {
                get
                {
                    return true;
                }
            }
        }
    }
}