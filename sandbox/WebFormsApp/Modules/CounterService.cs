namespace WebFormsApp.Modules
{
    public class CounterService
    {
        private int _count;

        public int Increment() => ++_count;

        public void Reset()
        {
            _count = 0;
        }
    }
}
