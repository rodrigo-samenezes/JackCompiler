using System;
using System.IO;
using System.Text;

namespace Jack {
    public class VMWriter {
        public enum Segment {
            ARG, LOCAL, STATIC, THIS, THAT, POINTER, TEMP, CONST
        }

        public enum Command {
            ADD, SUB, NEG, EQ, GT, LT, AND, OR, NOT
        }
        private FileStream fs;
        private StreamWriter sw;

        public VMWriter (FileStream fileStream) {
            this.fs = fileStream;
            this.sw = new StreamWriter(this.fs, Encoding.UTF8);
        }

        public void WritePush(Segment segment, int index) {
            this.sw.WriteLine($"push {segment.VM_ToString()} {index}");
        }

        public void WritePop(Segment segment, int index) {
            this.sw.WriteLine($"pop {segment.ToString().ToLower()} {index}");
        }

        public void WriteArithmetic(Command command) {
            this.sw.WriteLine($"{command.ToString().ToLower()}");
        }

        public void WriteLabel(string label) {
            this.sw.WriteLine($"label {label}");
        }

        public void WriteGoto(string label) {
            this.sw.WriteLine($"goto {label}");
        }
        public void WriteIf(string label) {
            this.sw.WriteLine($"if-goto {label}");
        }
        public void WriteCall(string name, int nArgs) {
            this.sw.WriteLine($"call {name} {nArgs}");
        }

        public void WriteFunction(string name, int nLocals) {
            this.sw.WriteLine($"function {name} {nLocals}");
        }

        public void WriteReturn() {
            this.sw.WriteLine($"return");
        }
        public void Close() {
            this.sw.Close();
            this.fs.Close();
        }

        public void WriteComment(string comment) {
            if (false) {
                this.sw.WriteLine($"//{comment}");
            }
        }

        
    }

    public static class VMWriter_Segment_Extensions {

    public static string VM_ToString(this VMWriter.Segment segment) {
            if (segment == VMWriter.Segment.CONST) {
                return "constant";
            }
            else if (segment == VMWriter.Segment.ARG) {
                return "argument";
            }
            else {
                return segment.ToString().ToLower();
            }
        }
    }
}