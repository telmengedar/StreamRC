using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;

namespace StreamRC.Streaming.Text {
    public class TextSpecs {

        public TextSpecs(string text, float size)
            : this(text, size, System.Drawing.Color.White)
        {
        }

        public TextSpecs(string text, float size, Color color)
        {
            Text = text;
            Size = size;
            Color = color;
        }

        public string Text { get; }

        public float Size { get; }

        public Color Color { get; }

        public int OutlineThickness { get; set; }

        public Color OutlineColor { get; set; }

        sealed class TextSpecsEqualityComparer : IEqualityComparer<TextSpecs> {
            public bool Equals(TextSpecs x, TextSpecs y) {
                if(ReferenceEquals(x, y)) return true;
                if(ReferenceEquals(x, null)) return false;
                if(ReferenceEquals(y, null)) return false;
                if(x.GetType() != y.GetType()) return false;
                return string.Equals(x.Text, y.Text) && Math.Abs(x.Size - y.Size)<0.001 && x.Color.Equals(y.Color) && x.OutlineThickness == y.OutlineThickness && x.OutlineColor.Equals(y.OutlineColor);
            }

            public int GetHashCode(TextSpecs obj) {
                unchecked {
                    var hashCode = (obj.Text != null ? obj.Text.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ obj.Size.GetHashCode();
                    hashCode = (hashCode * 397) ^ obj.Color.GetHashCode();
                    hashCode = (hashCode * 397) ^ obj.OutlineThickness;
                    hashCode = (hashCode * 397) ^ obj.OutlineColor.GetHashCode();
                    return hashCode;
                }
            }
        }

        static readonly IEqualityComparer<TextSpecs> TextSpecsComparerInstance = new TextSpecsEqualityComparer();
        public static IEqualityComparer<TextSpecs> TextSpecsComparer
        {
            get { return TextSpecsComparerInstance; }
        }
    }
}