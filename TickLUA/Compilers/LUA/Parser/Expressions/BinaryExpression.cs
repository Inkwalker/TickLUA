using System;
using System.Collections.Generic;
using TickLUA.Compilers.LUA.Lexer;
using TickLUA.VM;

namespace TickLUA.Compilers.LUA.Parser.Expressions
{
    internal class BinaryExpression : Expression
    {
        static readonly HashSet<BinaryOperation> POW = new HashSet<BinaryOperation>() { BinaryOperation.Pow };
        static readonly HashSet<BinaryOperation> MUL_DIV_MOD = new HashSet<BinaryOperation>() { BinaryOperation.Mul, BinaryOperation.Div, BinaryOperation.iDiv, BinaryOperation.Mod };
        static readonly HashSet<BinaryOperation> ADD_SUB = new HashSet<BinaryOperation>() { BinaryOperation.Add, BinaryOperation.Sub };
        static readonly HashSet<BinaryOperation> CONCAT = new HashSet<BinaryOperation>() { BinaryOperation.Concat };
        static readonly HashSet<BinaryOperation> COMP = new HashSet<BinaryOperation>() { BinaryOperation.Less, BinaryOperation.LessEq, BinaryOperation.Greater, BinaryOperation.GreaterEq, BinaryOperation.Equals, BinaryOperation.NotEquals };
        static readonly HashSet<BinaryOperation> LOGIC_AND = new HashSet<BinaryOperation>() { BinaryOperation.LogicAnd };
        static readonly HashSet<BinaryOperation> LOGIC_OR = new HashSet<BinaryOperation>() { BinaryOperation.LogicOr };

        public static object BeginOperatorChain()
        {
            return new LinkedList();
        }

        public static void AddExpressionToChain(object chain, Expression exp)
        {
            LinkedList list = (LinkedList)chain;
            Node node = new Node() { Expr = exp };
            AddNode(list, node);
        }


        public static void AddOperatorToChain(object chain, Token op)
        {
            LinkedList list = (LinkedList)chain;
            Node node = new Node() { Op = ParseBinaryOperator(op) };
            AddNode(list, node);
        }

        public static Expression CommitOperatorChain(object chain)
        {
            return CreateSubTree((LinkedList)chain);
        }

        private static void AddNode(LinkedList list, Node node)
        {
            list.OperatorMask.Add(node.Op);

            if (list.Nodes == null)
            {
                list.Nodes = list.Last = node;
            }
            else
            {
                list.Last.Next = node;
                node.Prev = list.Last;
                list.Last = node;
            }
        }

        private static Expression CreateSubTree(LinkedList list)
        {
            var opfound = list.OperatorMask;

            Node nodes = list.Nodes;

            if (opfound.Overlaps(POW))
                nodes = PrioritizeRightAssociative(nodes, POW);

            if (opfound.Overlaps(MUL_DIV_MOD))
                nodes = PrioritizeLeftAssociative(nodes, MUL_DIV_MOD);

            if (opfound.Overlaps(ADD_SUB))
                nodes = PrioritizeLeftAssociative(nodes, ADD_SUB);

            if (opfound.Overlaps(CONCAT))
                nodes = PrioritizeRightAssociative(nodes, CONCAT);

            if (opfound.Overlaps(COMP))
                nodes = PrioritizeLeftAssociative(nodes, COMP);

            if (opfound.Overlaps(LOGIC_AND))
                nodes = PrioritizeLeftAssociative(nodes, LOGIC_AND);

            if (opfound.Overlaps(LOGIC_OR))
                nodes = PrioritizeLeftAssociative(nodes, LOGIC_OR);


            if (nodes.Next != null || nodes.Prev != null)
                throw new Exception("Expression reduction didn't work! - 1");
            if (nodes.Expr == null)
                throw new Exception("Expression reduction didn't work! - 2");

            return nodes.Expr;
        }

        private static Node PrioritizeLeftAssociative(Node nodes, HashSet<BinaryOperation> operatorsToFind)
        {
            for (Node N = nodes; N != null; N = N.Next)
            {
                BinaryOperation o = N.Op;

                if (operatorsToFind.Contains(o))
                {
                    N.Op = BinaryOperation.Invalid;
                    N.Expr = new BinaryExpression(N.Prev.Expr, N.Next.Expr, o);
                    N.Prev = N.Prev.Prev;
                    N.Next = N.Next.Next;

                    if (N.Next != null)
                        N.Next.Prev = N;

                    if (N.Prev != null)
                        N.Prev.Next = N;
                    else
                        nodes = N;
                }
            }

            return nodes;
        }

        private static Node PrioritizeRightAssociative(Node nodes, HashSet<BinaryOperation> operatorsToFind)
        {
            Node last;
            for (last = nodes; last.Next != null; last = last.Next) { }

            for (Node N = last; N != null; N = N.Prev)
            {
                BinaryOperation o = N.Op;

                if (operatorsToFind.Contains(o))
                {
                    N.Op = BinaryOperation.Invalid;
                    N.Expr = new BinaryExpression(N.Prev.Expr, N.Next.Expr, o);
                    N.Prev = N.Prev.Prev;
                    N.Next = N.Next.Next;

                    if (N.Next != null)
                        N.Next.Prev = N;

                    if (N.Prev != null)
                        N.Prev.Next = N;
                    else
                        nodes = N;
                }
            }

            return nodes;
        }

        private Expression left, right;
        private BinaryOperation operation;

        public BinaryExpression(Expression left, Expression right, BinaryOperation op)
        {
            this.left = left;
            this.right = right;
            this.operation = op;
        }

        public BinaryExpression(Expression left, Expression right, Token token)
        {
            this.left = left;
            this.right = right;
            this.operation = ParseBinaryOperator(token);
        }

        private static BinaryOperation ParseBinaryOperator(Token token)
        {
            switch (token.Type)
            {
                case TokenType.OP_LogicOr:
                    return BinaryOperation.LogicOr;
                case TokenType.OP_LogicAnd:
                    return BinaryOperation.LogicAnd;
                case TokenType.OP_LessThan:
                    return BinaryOperation.Less;
                case TokenType.OP_GreaterThan:
                    return BinaryOperation.Greater;
                case TokenType.OP_LessEq:
                    return BinaryOperation.LessEq;
                case TokenType.OP_GreaterEq:
                    return BinaryOperation.GreaterEq;
                case TokenType.OP_NotEquals:
                    return BinaryOperation.NotEquals;
                case TokenType.OP_Equals:
                    return BinaryOperation.Equals;
                case TokenType.OP_Concat:
                    return BinaryOperation.Concat;
                case TokenType.OP_Add:
                    return BinaryOperation.Add;
                case TokenType.OP_Sub:
                    return BinaryOperation.Sub;
                case TokenType.OP_Mul:
                    return BinaryOperation.Mul;
                case TokenType.OP_Div:
                    return BinaryOperation.Div;
                case TokenType.OP_iDiv:
                    return BinaryOperation.iDiv;
                case TokenType.OP_Mod:
                    return BinaryOperation.Mod;
                case TokenType.OP_Pow:
                    return BinaryOperation.Pow;
                default:
                    throw new CompilationException($"Unexpected binary operator '{token.Content}'", token.Line, token.Column);
            }
        }

        private static void WriteInstructions(FunctionBuilder builder, BinaryOperation op, byte reg_res, byte reg_l, byte reg_r)
        {
            switch (op)
            {
                case BinaryOperation.LogicOr:
                case BinaryOperation.LogicAnd:
                case BinaryOperation.Less:
                    builder.AddInstruction(Instruction.LT(reg_l, reg_r, false));
                    builder.AddInstruction(Instruction.LOAD_FALSE_SKIP(reg_res));
                    builder.AddInstruction(Instruction.LOAD_TRUE(reg_res));
                    return;
                case BinaryOperation.Greater:
                    builder.AddInstruction(Instruction.LE(reg_l, reg_r, true));
                    builder.AddInstruction(Instruction.LOAD_FALSE_SKIP(reg_res));
                    builder.AddInstruction(Instruction.LOAD_TRUE(reg_res));
                    return;
                case BinaryOperation.LessEq:
                    builder.AddInstruction(Instruction.LE(reg_l, reg_r, false));
                    builder.AddInstruction(Instruction.LOAD_FALSE_SKIP(reg_res));
                    builder.AddInstruction(Instruction.LOAD_TRUE(reg_res));
                    return;
                case BinaryOperation.GreaterEq:
                    builder.AddInstruction(Instruction.LT(reg_l, reg_r, true));
                    builder.AddInstruction(Instruction.LOAD_FALSE_SKIP(reg_res));
                    builder.AddInstruction(Instruction.LOAD_TRUE(reg_res));
                    return;
                case BinaryOperation.NotEquals:
                    builder.AddInstruction(Instruction.EQ(reg_l, reg_r, true));
                    builder.AddInstruction(Instruction.LOAD_FALSE_SKIP(reg_res));
                    builder.AddInstruction(Instruction.LOAD_TRUE(reg_res));
                    return;
                case BinaryOperation.Equals:
                    builder.AddInstruction(Instruction.EQ(reg_l, reg_r, false));
                    builder.AddInstruction(Instruction.LOAD_FALSE_SKIP(reg_res));
                    builder.AddInstruction(Instruction.LOAD_TRUE(reg_res));
                    return;
                case BinaryOperation.Concat:
                case BinaryOperation.Add:
                    builder.AddInstruction(Instruction.ADD(reg_res, reg_l, reg_r));
                    return;
                case BinaryOperation.Sub:
                    builder.AddInstruction(Instruction.SUB(reg_res, reg_l, reg_r)); 
                    return;
                case BinaryOperation.Mul:
                    builder.AddInstruction(Instruction.MUL(reg_res, reg_l, reg_r));
                    return;
                case BinaryOperation.Div:
                    builder.AddInstruction(Instruction.DIV(reg_res, reg_l, reg_r));
                    return;
                case BinaryOperation.iDiv:
                    builder.AddInstruction(Instruction.IDIV(reg_res, reg_l, reg_r));
                    return;
                case BinaryOperation.Mod:
                    builder.AddInstruction(Instruction.MOD(reg_res, reg_l, reg_r));
                    return;
                case BinaryOperation.Pow:
                    builder.AddInstruction(Instruction.POW(reg_res, reg_l, reg_r));
                    return;
                default:
                    throw new CompilationException($"Unexpected binary operator", 1, 1);
            }
        }

        public override byte CompileRead(FunctionBuilder builder)
        {
            if (operation != BinaryOperation.Invalid)
            {
                var reg_r = right.CompileRead(builder);
                var reg_l = left.CompileRead(builder);

                byte reg_res = builder.AllocateRegisters(1);

                WriteInstructions(
                    builder,
                    operation, 
                    reg_res, 
                    reg_l,
                    reg_r 
                );

                ResultRegister = reg_res;

                right.ReleaseRegisters(builder);
                left.ReleaseRegisters(builder);
            }
            return 0;
        }

        public override void ReleaseRegisters(FunctionBuilder builder)
        {
            if (ResultRegister < 0) return;

            builder.DeallocateRegisters((byte)ResultRegister, 1);
        }

        class Node
        {
            public Expression Expr;
            public BinaryOperation Op;
            public Node Prev;
            public Node Next;
        }

        class LinkedList
        {
            public Node Nodes;
            public Node Last;
            public HashSet<BinaryOperation> OperatorMask = new HashSet<BinaryOperation>();
        }
    }
}
