namespace Keeper.Warm
{
    public enum Opcode
    {
        NoOperand = 0x01 << 24,
        Int32Operand = 0x02 << 24,
        RulePointerOperand = 0x03 << 24,
        OperandMask = 0xFF << 24,

        Halt = NoOperand | 0 << 8 | 0,
        Duplicate = NoOperand | 0 << 8 | 1,
        Allocate = Int32Operand | 0 << 8 | 2,
        Deallocate = Int32Operand | 0 << 8 | 3,
        Call = RulePointerOperand | 0 << 8 | 4,
        Proceed = NoOperand | 0 << 8 | 5,
        Deref = NoOperand | 0 << 8 | 6,
        GetTag = NoOperand | 0 << 8 | 7,
        Bind = NoOperand | 0 << 8 | 8,
        GetAddress = NoOperand | 0 << 8 | 9,
        Fail = NoOperand | 0 << 8 | 10,
        Unify = NoOperand | 0 << 8 | 11,
        Nop = NoOperand | 0 << 8 | 12,
        ChoicePoint = Int32Operand | 0 << 8 | 13,
        GetLevel = NoOperand | 0 << 8 | 14,
        Cut = NoOperand | 0 << 8 | 15,
        Trace = Int32Operand | 0 << 8 | 16,
        Callback = Int32Operand | 0 << 8 | 17,
        Pop = NoOperand | 0 << 8 | 18,
        EndTrace = Int32Operand | 0 << 8 | 19,

        LoadGlobalRegisterBase = NoOperand | 1 << 8,
        LoadGlobalRegisterH = LoadGlobalRegisterBase | GlobalRegister.TopOfHeap,
        LoadGlobalRegisterB0 = LoadGlobalRegisterBase | GlobalRegister.ChoicePointBase,

        ApplyTagBase = NoOperand | 2 << 8,
        ApplyTagRef = ApplyTagBase | Tag.Ref,
        ApplyTagStr = ApplyTagBase | Tag.Str,
        ApplyTagFun = ApplyTagBase | Tag.Fun,
        ApplyTagCon = ApplyTagBase | Tag.Con,
        ApplyTagLis = ApplyTagBase | Tag.Lis,

        LoadConstantBase = NoOperand | 3 << 8,
        LoadConstant0 = LoadConstantBase | 0,
        LoadConstant1 = LoadConstantBase | 1,
        LoadConstant2 = LoadConstantBase | 2,
        LoadConstant3 = LoadConstantBase | 3,
        LoadConstant4 = LoadConstantBase | 4,
        LoadConstant5 = LoadConstantBase | 5,
        LoadConstant6 = LoadConstantBase | 6,
        LoadConstant7 = LoadConstantBase | 7,
        LoadConstant = Int32Operand | 3 << 8,

        Load = NoOperand | 4 << 8,

        Store = NoOperand | 5 << 8,

        LoadLocal = Int32Operand | 6 << 8,

        StoreLocal = Int32Operand | 7 << 8,

        ArithmeticBase = NoOperand | 8 << 8,
        Increment = ArithmeticBase | 0,
        Add = ArithmeticBase | 1,

        LoadArgumentAddressBase = NoOperand | 9 << 8,
        LoadArgumentAddress0 = LoadArgumentAddressBase | 0,
        LoadArgumentAddress1 = LoadArgumentAddressBase | 1,
        LoadArgumentAddress2 = LoadArgumentAddressBase | 2,
        LoadArgumentAddress3 = LoadArgumentAddressBase | 3,
        LoadArgumentAddress4 = LoadArgumentAddressBase | 4,
        LoadArgumentAddress5 = LoadArgumentAddressBase | 5,
        LoadArgumentAddress6 = LoadArgumentAddressBase | 6,
        LoadArgumentAddress7 = LoadArgumentAddressBase | 7,

        BranchBase = Int32Operand | 10 << 8,
        BranchEqual = BranchBase | 0,
        BranchNotEqual = BranchBase | 1,
        BranchAlways = BranchBase | 2,
        BranchAbsolute = BranchBase | 3,

        StoreGlobalRegisterBase = NoOperand | 11 << 8,
        StoreGlobalRegisterH = StoreGlobalRegisterBase | GlobalRegister.TopOfHeap,
        StoreGlobalRegisterB0 = StoreGlobalRegisterBase | GlobalRegister.ChoicePointBase,

        LoadLocalAddress = Int32Operand | 12 << 8,
    }
}