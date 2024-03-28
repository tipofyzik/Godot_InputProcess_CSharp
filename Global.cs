using Godot;



[GlobalClass]
public partial class Global : Node {

	public static Global data { get; set; } = new Global() { };
	public InputHandler input_handler = new InputHandler() { };

}
