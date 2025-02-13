namespace Sales;

public class Manager
{
    public int Id { get; set; }
    public string Name { get; set; }

    public override string ToString() => Name; // Показывает имя в ComboBox
}
