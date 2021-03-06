﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SICXEAssembler
{
    /// <summary>
    /// A placeholder for a symbol specified in a program, to which the assembler will assign an address.
    /// </summary>
    internal class Symbol
    {
        static int _id = 0;
        public int ID
        { get; private set; }
        public string Name
        { get; private set; }
        public int? Address
        { get; set; }

        public Symbol(string name)
        {
            Name = name;
            ID = ++_id;
        }

        public override string ToString()
        {
            if (Address.HasValue)
                return $"{Name}@{Address.Value.ToString("X")}";
            else
                return $"{Name}@<not set>";
        }
    }

    class Literal : Symbol
    {
        public enum LiteralType
        {
            NotSet = 0,
            Hex = 1, // the literal is a hexadecimal number.
            Byte = 2 // the literal is given as ASCII text.
        }

        public LiteralType Type
        { get; private set; }

        public byte[] Data
        { get; private set; }

        public Literal(string name) : base(name)
        {
            if (!StringIsLiteralName(name))
                throw new ArgumentException(name);

            char type = char.ToLower(name[1]);
            string payload = name.Substring(3, name.Length - 4); // probably off by one or so.
            if (type == 'x')
            {
                Type = LiteralType.Hex;
                if ((payload.Length & 1) > 0)
                {
                    //Console.Error.WriteLine("Warning: Hex literal contains uneven number of characters. The left will be padded with 0.");
                    payload = '0' + payload;
                }
                Data = GetBytesFromHexString(payload);
            }
            else if (type == 'c')
            {
                Type = LiteralType.Byte;
                Data = System.Text.Encoding.ASCII.GetBytes(payload);
            }
            else
            {
                throw new ArgumentException(name);
            }
        }

        public static byte[] GetBytesFromHexString(string str)
        {
            int len = str.Length;
            var ret = new byte[(int)Math.Ceiling(len / 2d)];
            for (int i = 0; i < len; i += 2)
            {
                int take = Math.Min(2, len - i);
                ret[i / 2] = byte.Parse(str.Substring(i, take), System.Globalization.NumberStyles.AllowHexSpecifier);
            }
            return ret;
        }

        public static bool StringIsLiteralName(string symbol)
        {
            var regex = new Regex("=[xc]'[^' ]+'", RegexOptions.IgnoreCase);
            return regex.IsMatch(symbol);
        }

    }
}
