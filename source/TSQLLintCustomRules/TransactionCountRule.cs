using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using TSQLLint.Core.Interfaces;

namespace TSQLLint.CustomRules
{
    public class TransactionCountRule : TSqlFragmentVisitor, ISqlRule
    {
        private readonly Action<string, string, int, int> errorCallback;

        public TransactionCountRule(Action<string, string, int, int> errorCallback)
        {
            this.errorCallback = errorCallback;
        }

        public string RULE_NAME => "transactions-begin-commit-count";

        public string RULE_TEXT => "Transaction Missing Commit";

        public int DynamicSqlStartColumn { get; set; }
        public int DynamicSqlStartLine { get; set; }

        public override void Visit(TSqlBatch node)
        {
            var childTransactionVisitor = new ChildTransactionVisitor();
            node.Accept(childTransactionVisitor);


            if (childTransactionVisitor.TransactionLists.Exists(x => x.Commit == null))
            {
                var failed_transaction = childTransactionVisitor.TransactionLists.Where(x => x.Commit == null).First();

                errorCallback(RULE_NAME, RULE_TEXT, failed_transaction.Begin.StartLine, GetColumnNumber(failed_transaction));
            }
        }

        public class TrackedTransaction
        {
            public BeginTransactionStatement Begin { get; set; }

            public CommitTransactionStatement Commit { get; set; }
        }

        public class ChildTransactionVisitor : TSqlFragmentVisitor
        {
            public List<TrackedTransaction> TransactionLists { get; } = new List<TrackedTransaction>();

            public override void Visit(BeginTransactionStatement node)
            {
                TransactionLists.Add(new TrackedTransaction { Begin = node });
            }

            public override void Visit(CommitTransactionStatement node)
            {
                var firstUncomitted = TransactionLists.FirstOrDefault(x => x.Commit == null);
                if (firstUncomitted != null)
                {
                    firstUncomitted.Commit = node;
                }
            }
        }

        private int GetColumnNumber(TrackedTransaction transaction)
        {
            return transaction.Begin.StartLine == DynamicSqlStartLine
                ? transaction.Begin.StartColumn + DynamicSqlStartColumn
                : transaction.Begin.StartColumn;
        }
    }
}
