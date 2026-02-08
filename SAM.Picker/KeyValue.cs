/* Copyright (c) 2024 Rick (rick 'at' gibbed 'dot' us)
 *
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 *
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would
 *    be appreciated but is not required.
 *
 * 2. Altered source versions must be plainly marked as such, and must not
 *    be misrepresented as being the original software.
 *
 * 3. This notice may not be removed or altered from any source
 *    distribution.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SAM.Picker
{
    internal class KeyValue
    {
        private static readonly KeyValue _Invalid = new();
        public string Name = "<root>";
        public KeyValueType Type = KeyValueType.None;
        public object Value;
        public bool Valid;

        public List<KeyValue> Children = null;

        public KeyValue this[string key]
        {
            get
            {
                if (this.Children == null)
                {
                    return _Invalid;
                }

                var child = this.Children.SingleOrDefault(
                    c => string.Compare(c.Name, key, StringComparison.InvariantCultureIgnoreCase) == 0);

                if (child == null)
                {
                    return _Invalid;
                }

                return child;
            }
        }

        public string AsString(string defaultValue)
        {
            if (this.Valid == false)
            {
                return defaultValue;
            }

            if (this.Value == null)
            {
                return defaultValue;
            }

            return this.Value.ToString();
        }

        public int AsInteger(int defaultValue)
        {
            if (this.Valid == false)
            {
                return defaultValue;
            }

            switch (this.Type)
            {
                case KeyValueType.String:
                case KeyValueType.WideString:
                {
                    return int.TryParse((string)this.Value, out int value) == false
                        ? defaultValue
                        : value;
                }

                case KeyValueType.Int32:
                {
                    return (int)this.Value;
                }

                case KeyValueType.Float32:
                {
                    return (int)((float)this.Value);
                }

                case KeyValueType.UInt64:
                {
                    return (int)((ulong)this.Value & 0xFFFFFFFF);
                }
            }

            return defaultValue;
        }

        public float AsFloat(float defaultValue)
        {
            if (this.Valid == false)
            {
                return defaultValue;
            }

            switch (this.Type)
            {
                case KeyValueType.String:
                case KeyValueType.WideString:
                {
                    return float.TryParse((string)this.Value, out float value) == false
                        ? defaultValue
                        : value;
                }

                case KeyValueType.Int32:
                {
                    return (int)this.Value;
                }

                case KeyValueType.Float32:
                {
                    return (float)this.Value;
                }

                case KeyValueType.UInt64:
                {
                    return (ulong)this.Value & 0xFFFFFFFF;
                }
            }

            return defaultValue;
        }

        public bool AsBoolean(bool defaultValue)
        {
            if (this.Valid == false)
            {
                return defaultValue;
            }

            switch (this.Type)
            {
                case KeyValueType.String:
                case KeyValueType.WideString:
                {
                    return int.TryParse((string)this.Value, out int value) == false
                        ? defaultValue
                        : value != 0;
                }

                case KeyValueType.Int32:
                {
                    return ((int)this.Value) != 0;
                }

                case KeyValueType.Float32:
                {
                    return ((int)((float)this.Value)) != 0;
                }

                case KeyValueType.UInt64:
                {
                    return ((ulong)this.Value) != 0;
                }
            }

            return defaultValue;
        }

        public override string ToString()
        {
            if (this.Valid == false)
            {
                return "<invalid>";
            }

            if (this.Type == KeyValueType.None)
            {
                return this.Name;
            }

            return $"{this.Name} = {this.Value}";
        }

        public static KeyValue LoadAsBinary(string path)
        {
            using (BinaryReader reader = new(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                return LoadAsBinary(reader);
            }
        }

        private static KeyValue LoadAsBinary(BinaryReader reader)
        {
            var root = new KeyValue();
            root.Children = new();

            while (true)
            {
                var child = LoadAsBinaryRecursive(reader);
                if (child == null)
                {
                    break;
                }

                root.Children.Add(child);
            }

            return root;
        }

        private static KeyValue LoadAsBinaryRecursive(BinaryReader reader)
        {
            var type = (KeyValueType)reader.ReadByte();

            if (type == KeyValueType.End)
            {
                return null;
            }

            var kv = new KeyValue
            {
                Type = type,
                Name = ReadNullTerminatedString(reader),
                Valid = true,
            };

            if (type == KeyValueType.None)
            {
                kv.Children = new();

                while (true)
                {
                    var child = LoadAsBinaryRecursive(reader);
                    if (child == null)
                    {
                        break;
                    }

                    kv.Children.Add(child);
                }

                return kv;
            }

            kv.Value = type switch
            {
                KeyValueType.String => ReadNullTerminatedString(reader),
                KeyValueType.Int32 => reader.ReadInt32(),
                KeyValueType.Float32 => reader.ReadSingle(),
                KeyValueType.Pointer => reader.ReadInt32(),
                KeyValueType.WideString => ReadNullTerminatedString(reader, true),
                KeyValueType.Color => reader.ReadUInt32(),
                KeyValueType.UInt64 => reader.ReadUInt64(),
                _ => throw new InvalidOperationException("invalid keyvalue type"),
            };

            return kv;
        }

        private static string ReadNullTerminatedString(BinaryReader reader, bool wide = false)
        {
            if (wide == false)
            {
                var bytes = new List<byte>();
                while (true)
                {
                    byte b = reader.ReadByte();
                    if (b == 0)
                    {
                        break;
                    }

                    bytes.Add(b);
                }
                return System.Text.Encoding.UTF8.GetString(bytes.ToArray());
            }

            var chars = new List<char>();
            while (true)
            {
                char c = reader.ReadChar();
                if (c == 0)
                {
                    break;
                }

                chars.Add(c);
            }
            return new string(chars.ToArray());
        }
    }
}
