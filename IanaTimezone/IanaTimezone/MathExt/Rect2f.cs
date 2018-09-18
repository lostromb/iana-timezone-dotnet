using System;

namespace Iana.Timezone.MathExt
{
    public struct Rect2f
    {
        private float minX;
        private float minY;
        private float maxX;
        private float maxY;
        
        public Rect2f(float x, float y, float width, float height)
        {
            this.minX = x;
            this.minY = y;
            this.maxX = x + width;
            this.maxY = y + height;
        }

        public float X
        {
            get
            {
                return this.minX;
            }
        }

        public float Y
        {
            get
            {
                return this.minY;
            }
        }

        public float MaxX
        {
            get
            {
                return this.maxX;
            }
        }

        public float MaxY
        {
            get
            {
                return this.maxY;
            }
        }

        public float Width
        {
            get
            {
                return this.maxX - this.minX;
            }
        }

        public float Height
        {
            get
            {
                return this.maxY - this.minY;
            }
        }

        public float CenterX
        {
            get
            {
                return this.minX + (this.maxX - this.minX) / 2;
            }
        }

        public float CenterY
        {
            get
            {
                return this.minY + (this.maxY - this.minY) / 2;
            }
        }

        public bool Intersects(Rect2f other)
        {
            throw new NotImplementedException();
        }

        public bool Contains(Rect2f rect)
        {
            return rect.X >= this.X &&
                    rect.Y >= this.Y &&
                    rect.MaxX < this.MaxX &&
                    rect.MaxY < this.MaxY;
        }

        public bool Contains(Vector2f point)
        {
            return point.X >= this.X &&
                    point.Y >= this.Y &&
                    point.X < this.MaxX &&
                    point.Y < this.MaxY;
        }
    }
}
