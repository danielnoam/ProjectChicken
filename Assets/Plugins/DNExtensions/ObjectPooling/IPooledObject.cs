

namespace DNExtensions
{
    public interface IPooledObject
    {
        void OnPoolGet();
        void OnPoolReturn();
        void OnPoolRecycle();
    }

}