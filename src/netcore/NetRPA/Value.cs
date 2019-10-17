
namespace NetRPA{


    public class Value{
        internal object obj;

        public object GetOriginalValue()
        {
            return obj;
        }


    }
    public class Value<T>: Value{


        public Value(T obj){
            this.obj = obj;
        }

        public new T GetOriginalValue(){
            return (T)obj;
        }

    }
}