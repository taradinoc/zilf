using Zilf.Emit;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;

namespace Zilf.Compiler
{
    internal class Action
    {
        public readonly int Index;
        public readonly IOperand Constant;
        public readonly IRoutineBuilder Routine, PreRoutine;
        public readonly ZilAtom RoutineName, PreRoutineName;

        public Action(int index, IOperand constant, IRoutineBuilder routine, IRoutineBuilder preRoutine,
            ZilAtom routineName, ZilAtom preRoutineName)
        {
            this.Index = index;
            this.Constant = constant;
            this.Routine = routine;
            this.RoutineName = routineName;
            this.PreRoutine = preRoutine;
            this.PreRoutineName = preRoutineName;
        }
    }
}
