using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Collections.Generic;
using System.Text;
using TSQLLint.Core.Interfaces;

namespace TSQLLintCustomRules
{
    public class MissingWhereRule : TSqlFragmentVisitor, ISqlRule
    {
        private readonly Action<string, string, int, int> errorCallback;

        public MissingWhereRule(Action<string, string, int, int> errorCallback)
        {
            this.errorCallback = errorCallback;
        }

        public string RULE_NAME => "missing-where";

        public string RULE_TEXT => "statement should contain WHERE clause to keep the modification of records under control. Otherwise unexpected data loss could result.";

        public int DynamicSqlStartColumn { get; set; }
        public int DynamicSqlStartLine { get; set; }

        public override void Visit(UpdateStatement node)
        {
            var whereClauseVisitor = new WhereClauseVisitor();
            node.Accept(whereClauseVisitor);

            if (!whereClauseVisitor.whereClausefound)
            {
                errorCallback(RULE_NAME, "Update " + RULE_TEXT, node.StartLine, GetColumnNumber(node));
            }
        }

        public override void Visit(DeleteStatement node)
        {
            var whereClauseVisitor = new WhereClauseVisitor();
            node.Accept(whereClauseVisitor);

            if (whereClauseVisitor.whereClausefound)
            {
                errorCallback(RULE_NAME, "Delete " + RULE_TEXT, node.StartLine, GetColumnNumber(node));
            }

        }

        public class WhereClauseVisitor : TSqlFragmentVisitor
        {
            public bool whereClausefound;

            public override void Visit(WhereClause node)
            {
                whereClausefound = true;
            }
        }

        private int GetColumnNumber(DataModificationStatement update)
        {
            return update.StartLine == DynamicSqlStartLine
                ? update.StartColumn + DynamicSqlStartColumn
                : update.StartColumn;
        }
    }
}
