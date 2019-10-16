class Test{
    public int Invoke(int input){
        return Helper.Add7(input);
    }
}

static class Helper{
    public static int Add7(int v)
    {
        return v + 7;
    }
}