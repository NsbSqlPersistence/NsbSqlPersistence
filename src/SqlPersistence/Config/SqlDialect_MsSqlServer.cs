namespace NServiceBus
{
    using System;
    using System.Data;
    using System.Data.Common;

    public abstract partial class SqlDialect
    {
        /// <summary>
        /// Microsoft SQL Server
        /// </summary>
        public partial class MsSqlServer : SqlDialect
        {
            /// <summary>
            /// Microsoft SQL Server
            /// </summary>
            public MsSqlServer()
            {
                Schema = "dbo";
            }

            internal override void AddCreationScriptParameters(DbCommand command)
            {
                command.AddParameter("schema", Schema);
            }

            internal override void SetJsonParameterValue(DbParameter parameter, object value)
            {
                SetParameterValue(parameter, value);
            }

            internal override void SetParameterValue(DbParameter parameter, object value)
            {
                if (value is ArraySegment<char> charSegment)
                {
                    parameter.Value = charSegment.Array;
                    parameter.Size = charSegment.Count;
                }
                else
                {
                    parameter.Value = value;
                }
            }

            internal override CommandWrapper CreateCommand(DbConnection connection)
            {
                var command = connection.CreateCommand();
                return new CommandWrapper(command, this);
            }

            internal override CommandBehavior ModifyBehavior(DbConnection connection, CommandBehavior baseBehavior)
            {
                if (!hasConnectionBeenInspectedForEncryption)
                {
                    isConnectionEncrypted = connection.IsEncrypted();
                    hasConnectionBeenInspectedForEncryption = true;
                }

                if (isConnectionEncrypted)
                {
                    baseBehavior &= ~CommandBehavior.SequentialAccess; //Remove sequential access
                }

                return baseBehavior;
            }

            internal override object GetCustomDialectDiagnosticsInfo()
            {
                return new
                {
                    CustomSchema = string.IsNullOrEmpty(Schema),
                    DoNotUseTransportConnection
                };
            }

            internal string Schema { get; set; }
            bool hasConnectionBeenInspectedForEncryption;
            bool isConnectionEncrypted;
        }
    }
}