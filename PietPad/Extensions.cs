using PietPad.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PietPad
{
    public static class Extensions
    {
        public static string GetName(this Operation operation)
        {
            switch (operation)
            {
                case Operation.nop:
                    return string.Empty;
                case Operation.in_char:
                    return "in(char)";
                case Operation.out_char:
                    return "out(char)";
                case Operation.in_number:
                    return "in(num)";
                case Operation.out_number:
                    return "out(num)";
                case Operation.@switch:
                    return "switch";
                default:
                    return operation.ToString();
            }
        }
    }
}
