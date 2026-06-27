namespace ProjectA;

public class Class
{
    public static Class FactoryMethod() => new();

    public void InstanceMethod()
    {
        // Recursive call
        if (Random.Shared.Next(100) < 50)
        {
            InstanceMethod();
        }
    }

    public static void StaticMethod()
    {
        // Recursive class
        if (Random.Shared.Next(100) < 50)
        {
            StaticMethod();
        }
    }

    public void SelfReferences()
    {
        var instance = new Class();

        instance.InstanceMethod();

        Class.StaticMethod();
    }
}