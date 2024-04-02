# Implementation of the processing input via C# events in Godot Engine
The [Godot](https://godotengine.org/) is an open-source, free game engine. It's popular among the indie-gamedevelopers. I write my code in C# since every popular engine requires this programming language.

## Introduction to the problem
Recently, I started doing my project, however, I forced with the fact that Godot can't handle input via signals. The developer, usually, handles input from the keyboard and mouse via if-statements in a _Process function that checks statements every engine tick (typically it is 60 physics ticks per second). 

**But what's the problem?** Imagine that you have your player and let's say it has 4 special buttons: space - jump, shift - dash, mouse leftclick - fire, E-button - interact with object. Right now, you are adding this action to the input map and checking them to see if anything was pressed (code example below)
```
	public override void _Process(double delta) {
		get_input();  //This function gets basic control (left, right, up, down) and sets the player velocity
		if (Input.IsActionJustPressed("jump")) {
			jump();
		}
		else if (Input.IsActionJustPressed("dash")) {
			dash();
		}
		else if (Input.IsActionJustPressed("fire")) {
			fire();
		}
		else if (Input.IsActionJustPressed("interact_with_object")) {
			interact_with_object();
		}
		MoveAndSlide();  //This function is responsible for character moving (see Godot documentation)
}

```
So, every tick the engine check 4 conditions in the worst case. Now, imagine that you have 3 entities and they also have for 2 conditions each. So, every tick Godot checks 4+3*2=10 conditions in the worst case. In short, the larger your game, the more conditions the game engine checks. I want to optimize this process by implementing the InputHandler class that would take an input and then send the corresponding signal to the game entities (for example, player).

## Implementation
**Code is written in C#, you can transfer it in gd-script (Godot built-in language).**

**The idea**: The best thing is to read all input that the engine gets from the user and then filter it according to the keybinds, i.e., process only keys that has are connetcted to their own actions.  
**The result**: In the inplementation, I don't use input map. The main drawback here is that you need to set all your actions manually (I'll explain how to do so a little bit later). 
**Note**: I wrote this code for 2D game and I believe, everithing you need to do to make it work for 3D game is to replace Node2D : Godot.Node2D by Node3D : Godot.Node3D in the _Input method (but it should be checked, since I didn't test this). See explanation below


**How it works**:  
First you need to do is to create InputHandler class 
First you need to do is to override an _Inpit method in the built-in Node2D class (represents a 2D object).
'''
public partial class Node2D : Godot.Node2D {
    public override void _Input(InputEvent @event) {
        if (@event is InputEventKey || @event is InputEventMouseButton) {
            Global.data.input_handler.process_key_state(@event.AsText(), @event.IsPressed());
        }
    }
}
'''
Here, the first filtering of buttons occurs. The _Input function, basically, calls when an input is detected (button/mouse click, mouse motion, gamepad stick moved,etc.). I require only button and mouse cliks, so here I just check whether the input suitable for me.    
