namespace __Product__.Core.Abstractions;

public interface IDataChangeNotifier
{
    void Notify(DataChanged change);
}
