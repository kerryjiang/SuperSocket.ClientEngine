namespace SuperSocket.ClientEngine.Protocol
{
    public interface ICommand
    {
        string Name { get; }
    }

    public interface ICommand<TSession, TPackageInfo> : ICommand
        where TPackageInfo : IPackageInfo
        where TSession : class
    {
        void ExecuteCommand(TSession session, TPackageInfo packageInfo);
    }
}