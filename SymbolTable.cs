using System;
using System.Collections.Generic;
using System.Linq;

namespace Jack
{

    public class SymbolTable
    {
        public enum Kind
        {
            STATIC,
            FIELD,
            ARG,
            VAR
        }

        public struct SymbolRow
        {
            public string type;
            public Kind kind;
            public int index;
        }

        private Dictionary<string, SymbolRow> classTable;
        private Dictionary<string, SymbolRow> subroutineTable;

        public SymbolTable()
        {
            this.classTable = new Dictionary<string, SymbolRow>();
            this.subroutineTable = new Dictionary<string, SymbolRow>();
        }

        public void StartSubroutine()
        {
            this.subroutineTable = new Dictionary<string, SymbolRow>();
        }

        public void Define(string name, string type, Kind kind)
        {
            int index = this.VarCount(kind);
            if (kind == Kind.FIELD || kind == Kind.STATIC)
            {
                this.classTable.Add(name, new SymbolRow
                {
                    type = type,
                    kind = kind,
                    index = index
                });
            }
            else {
                this.subroutineTable.Add(name, new SymbolRow
                {
                    type = type,
                    kind = kind,
                    index = index
                });
            }
        }

        public int VarCount(Kind kind)
        {
            var table = (kind == Kind.FIELD || kind == Kind.STATIC) ? this.classTable : this.subroutineTable;
            return (from x in table.Values where x.kind == kind select x).Count();
        }

        public Kind KindOf(string name)
        {
            if (this.subroutineTable.ContainsKey(name))
            {
                return this.subroutineTable[name].kind;
            }
            else if (this.classTable.ContainsKey(name)) {
                return this.classTable[name].kind;
            }
            else 
            {
                throw new Exception($"var {name} not found");
            }
        }
        public string TypeOf(string name)
        {
            if (this.subroutineTable.ContainsKey(name))
            {
                return this.subroutineTable[name].type;
            }
            else if (this.classTable.ContainsKey(name)) {
                return this.classTable[name].type;
            }
            else 
            {
                return null;
            }
        }
        public int IndexOf(string name)
        {
            if (this.subroutineTable.ContainsKey(name))
            {
                return this.subroutineTable[name].index;
            }
            else if (this.classTable.ContainsKey(name)) {
                return this.classTable[name].index;
            }
            else 
            {
                return -1;
            }
        }
    }
}