﻿module WebSharper.UI.Next.Tests.TodoList

open IntelliFactory.WebSharper
open IntelliFactory.WebSharper.Html

open IntelliFactory.WebSharper.UI.Next.RDom
module RVa = IntelliFactory.WebSharper.UI.Next.Reactive.Var
module RVi = IntelliFactory.WebSharper.UI.Next.Reactive.View
module RO = IntelliFactory.WebSharper.UI.Next.Reactive.Observation
module RC = IntelliFactory.WebSharper.UI.Next.ReactiveCollection.ReactiveCollection

open IntelliFactory.WebSharper.UI.Next.Reactive
open IntelliFactory.WebSharper.UI.Next.ReactiveCollection.ReactiveCollection

[<JavaScript>]
let el name xs = Element name [Attrs.Empty] xs

[<JavaScript>]
module TodoList =
    type TodoItem = { TodoText : string ; Done : bool }
    let mkTodo s = { TodoText = s ; Done = false }

    // Remove an item from the todo list.
    let rec removeItem (item : TodoItem) (lst : TodoItem list) =
        match lst with
        | [] -> []
        | x :: xs when x = item -> xs
        | x :: xs -> x :: (removeItem item xs)

    // Add an item to the end of the todo list
    let addItem (item : TodoItem) (lst : TodoItem list) = lst @ [item]

    let renderItemVar (coll: ReactiveCollection<Var<TodoItem>>) (todoVar: Var<TodoItem>) =
        let view = RVi.Create todoVar
        RVi.Map
            (fun todo ->
                el "div" [
                    (if (todo.Done) then
                        el "del" [ TextNode todo.TodoText ]
                     else
                        TextNode todo.TodoText)

                    Button "Done"
                        (fun _ -> RVa.Set todoVar {todo with Done = true})

                    Button "Remove"
                        (fun _ -> RC.RemoveVar coll todoVar)
                ]) view |> EmbedView

    let todoList coll =
        el "div" [
            RenderCollection coll renderItemVar
        ]

    let todoForm coll =
        let rvInput = RVa.Create ""
        let rviInput = RVi.Create rvInput

        el "div" [
            TextNode "New entry: "
            Input rvInput
            Button "Submit"
                (fun _ ->
                    let rvNewTodo =
                        rviInput
                        |> RVi.Now
                        |> mkTodo
                        |> RVa.Create
                    RC.AddVar coll rvNewTodo)
        ]

    let todoExample =
        let rc = RC.CreateReactiveCollection [] (RVa.GetKey)
        el "div" [
            todoList rc
            todoForm rc
        ]

    let main () =
        RunById "main" todoExample
        Div []
