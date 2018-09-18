namespace Iana.Timezone.MathExt
{
    public struct Vector2f
    {
        public float X;
        public float Y;

        public Vector2f(float x, float y)
        {
            X = x;
            Y = y;
        }

        public void SetLocation(float x, float y)
        {
            X = x;
            Y = y;
        }

        public float Distance(Vector2f other)
        {
            float dx = other.X - X;
            float dy = other.Y - Y;
            return (float)System.Math.Sqrt((dx * dx) + (dy * dy));
        }

        public Vector2f Add(Vector2f other)
        {
            return Add(other.X, other.Y);
        }

        public Vector2f Add(float x, float y)
        {
            return new Vector2f(X + x, Y + y);
        }

        public Vector2f Subtract(Vector2f other)
        {
            return Subtract(other.X, other.Y);
        }

        public Vector2f Subtract(float x, float y)
        {
            return new Vector2f(X - x, Y - y);
        }

        public Vector2f Multiply(Vector2f other)
        {
            return Multiply(other.X, other.Y);
        }

        public Vector2f Multiply(float x, float y)
        {
            return new Vector2f(X * x, Y * y);
        }

        public Vector2f Multiply(float w)
        {
            return new Vector2f(X * w, Y * w);
        }

        public float Magnitude
        {
            get
            {
                return (float)System.Math.Sqrt((X * X) + (Y * Y));
            }
            set
            {
                if (X != 0 || Y != 0)
                {
                    float factor = value / Magnitude;
                    X *= factor;
                    Y *= factor;
                }
            }
        }

        public Vector2f Clone()
        {
            return new Vector2f(X, Y);
        }

        public static readonly Vector2f Zero = new Vector2f(0, 0);
    }
}
