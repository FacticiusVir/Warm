namespace Keeper.Warm
{
    public enum Opcode
    {
        NoOperand = 0x01 << 24,
        Int64Operand = 0x02 << 24,
        MethodTokenOperand = 0x03 << 24,
        OperandMask = 0xFF << 24,

        Halt = NoOperand | 0 << 8 | 0,
        Duplicate = NoOperand | 0 << 8 | 1,
        Call = MethodTokenOperand | 0 << 8 | 4,
        Proceed = NoOperand | 0 << 8 | 5,
        Fail = NoOperand | 0 << 8 | 10,
        Nop = NoOperand | 0 << 8 | 12,
        ChoicePoint = Int64Operand | 0 << 8 | 13,

        LoadPointerBase = NoOperand | 2 << 8,
        LoadPointerHeap = LoadPointerBase | (int)AddressType.Heap,
        LoadPointerRetained = LoadPointerBase | (int)AddressType.Retained,

        LoadConstantBase = NoOperand | 3 << 8,
        LoadConstant0 = LoadConstantBase | 0,
        LoadConstant1 = LoadConstantBase | 1,
        LoadConstant2 = LoadConstantBase | 2,
        LoadConstant3 = LoadConstantBase | 3,
        LoadConstant4 = LoadConstantBase | 4,
        LoadConstant5 = LoadConstantBase | 5,
        LoadConstant6 = LoadConstantBase | 6,
        LoadConstant7 = LoadConstantBase | 7,
        LoadConstant = Int64Operand | 3 << 8,

        Load = NoOperand | 4 << 8,

        Store = NoOperand | 5 << 8,

        LoadLocal = Int64Operand | 6 << 8,

        StoreLocal = Int64Operand | 7 << 8,

        ArithmeticBase = NoOperand | 8 << 8,
        Increment = ArithmeticBase | 0,
        Add = ArithmeticBase | 1,
        Sub = ArithmeticBase | 2,
        Mul = ArithmeticBase | 3,
        Div = ArithmeticBase | 4,
        And = ArithmeticBase | 5,
        Or = ArithmeticBase | 6,
        Xor = ArithmeticBase | 7,
        Shl = ArithmeticBase | 8,
        Shr = ArithmeticBase | 9,

        LoadArgumentAddressBase = NoOperand | 9 << 8,
        LoadArgumentAddress0 = LoadArgumentAddressBase | 0,
        LoadArgumentAddress1 = LoadArgumentAddressBase | 1,
        LoadArgumentAddress2 = LoadArgumentAddressBase | 2,
        LoadArgumentAddress3 = LoadArgumentAddressBase | 3,
        LoadArgumentAddress4 = LoadArgumentAddressBase | 4,
        LoadArgumentAddress5 = LoadArgumentAddressBase | 5,
        LoadArgumentAddress6 = LoadArgumentAddressBase | 6,
        LoadArgumentAddress7 = LoadArgumentAddressBase | 7,

        BranchBase = Int64Operand | 10 << 8,
        BranchEqual = BranchBase | 0,
        BranchNotEqual = BranchBase | 1,
        BranchAlways = BranchBase | 2,

        LoadLocalAddress = Int64Operand | 12 << 8,

        LoadLocalBase = NoOperand | 13 << 8,
        LoadLocal0 = LoadLocalBase | 0,
        LoadLocal1 = LoadLocalBase | 1,
        LoadLocal2 = LoadLocalBase | 2,
        LoadLocal3 = LoadLocalBase | 3,
        LoadLocal4 = LoadLocalBase | 4,
        LoadLocal5 = LoadLocalBase | 5,
        LoadLocal6 = LoadLocalBase | 6,
        LoadLocal7 = LoadLocalBase | 7,

        StoreLocalBase = NoOperand | 14 << 8,
        StoreLocal0 = StoreLocalBase | 0,
        StoreLocal1 = StoreLocalBase | 1,
        StoreLocal2 = StoreLocalBase | 2,
        StoreLocal3 = StoreLocalBase | 3,
        StoreLocal4 = StoreLocalBase | 4,
        StoreLocal5 = StoreLocalBase | 5,
        StoreLocal6 = StoreLocalBase | 6,
        StoreLocal7 = StoreLocalBase | 7,
    }
}