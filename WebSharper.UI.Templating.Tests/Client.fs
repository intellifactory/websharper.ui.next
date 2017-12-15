namespace WebSharper.UI.Tests

open WebSharper
open WebSharper.JavaScript

open WebSharper.UI
open WebSharper.UI.Html
open WebSharper.UI.Notation
open WebSharper.UI.Templating

[<JavaScript>]
module Client =
    open WebSharper.UI.Client

    type MyTemplate = Template<"index.html,template.html", clientLoad = ClientLoad.FromDocument, legacyMode = LegacyMode.New>

    type Item =
        { id : int; name: string; description: string }
        static member Key x = x.id

    [<SPAEntryPoint>]
    let Main() =
        let myItems =
            ListModel.CreateWithStorage Item.Key (Storage.LocalStorage "Test" Serializer.Default)

        let newName = Var.Create ""
        let newDescr = Var.Create ""
        let itemsSub = Submitter.Create myItems.View Seq.empty
        let stitle = "Starting titlo"
        let var = Var.Create ""

        let title = 
            stitle
            |> Seq.toList
            |> List.map Var.Create

        async {
            do! Async.Sleep 1500
            Var.Set (List.nth title (title.Length - 1)) 'e'
        } |> Async.Start

        let tv = title
                 |> Seq.map View.FromVar
                 |> View.Sequence
                 |> View.Map (fun e -> new string(Seq.toArray e))
        let btnSub = Submitter.Create var.View ""
 
        let mutable lastKey = myItems.Length
        let freshKey() =
            lastKey <- lastKey + 1
            lastKey

        let findByKey = Var.Create ""
        let found = 
            findByKey.View.BindInner(fun s -> 
                myItems.TryFindByKeyAsView(int s).Map(function 
                    | None -> "none" 
                    | Some a -> a.name + ":" + a.description))

        let chk = Var.Create true

        let username = Var.Create ""
        let password = Var.Create ""
        let submit = Submitter.CreateOption (View.Map2 (fun x y -> x, y) username.View password.View)

        let testCounter = Var.Create 0
        let eltUpdater =
            div [ 
                Attr.DynamicStyle "background" (testCounter.View.Map(fun i -> if i % 2 = 0 then "white" else "lightgray"))
            ] [
                testCounter.View.Doc(fun _ -> Doc.Empty)
            ]
            |> Doc.ToUpdater
        let testCounterStr = testCounter.View.Map(string)
        let added = System.Collections.Generic.Queue<Elt>()
        let removed = System.Collections.Generic.Queue<Elt>()

        let addDiv () =
            let child =
                div [] [ textView testCounterStr ]  
            added.Enqueue child
            eltUpdater.Dom.AppendChild(child.Dom) |> ignore
            eltUpdater.AddUpdated child

        let removeUpdater () =
            if added.Count > 0 then
                let rem = added.Dequeue()
                eltUpdater.RemoveUpdated rem
                removed.Enqueue rem
                Console.Log "removed updater"

        let reAddUpdater () =
            if removed.Count > 0 then
                let readd = removed.Dequeue()
                eltUpdater.AddUpdated readd
                added.Enqueue readd
                Console.Log "readded updater"

        let removeAllUpdaters () =
            while added.Count > 0 do
                removed.Enqueue(added.Dequeue())    
            eltUpdater.RemoveAllUpdated()
            added.Clear()
            Console.Log "removed all updaters"

        let doc =
            MyTemplate.template()
                .Attr(Attr.Style "font-weight" "bold")
                .Title(
                    h1 [
                        attr.style "color: blue"
                        attr.classDynPred var.View (View.Const true)
                        on.click (fun el ev -> Console.Log ev)
                    ] [textView tv]
                )
                .ListContainer(
                    myItems.ViewState.DocSeqCached(Item.Key, fun key item ->
                        MyTemplate.template.ListItem()
                            .Key(item.Map(fun i -> string i.id))
                            .Name(item.Map(fun i -> i.name))
                            .Description(myItems.LensInto (fun i -> i.description) (fun i d -> { i with description = d }) key)
                            .FontStyle("italic")
                            .FontWeight("bold")
                            .Remove(fun _ -> myItems.RemoveByKey key)
                            .Elt()
                            .OnClickView(item, fun _ ev x -> Console.Log(x.name, ev.ClientX, ev.ClientY))
                    )
                )
                .NewName(newName)
                .LIKey("test1234")
                .LIFontStyle("italic")
                .LIName("liname")
                .MIAttr(Attr.Style "font-family" "monospace")
                .Class3("class3")
                .LIExtraAttr(Attr.Class "class4")
                .Replace2("Replace2")
                .NewDescription(newDescr)
                .NewItem(fun _ -> myItems.Add { id = freshKey(); name = newName.Value; description = newDescr.Value })
                .SubmitItems(fun _ -> itemsSub.Trigger())
                .ClearItems(fun _ -> myItems.Clear())
                .Test102(
                    // Test #102: this would empty the whole containing div
                    myItems.ViewState
                    |> Doc.BindSeqCached (fun x -> p [] [text x.description])
                )
                .Test106(
                    MyTemplate.template.Test106Tpl()
                        .DynamicReplace(
                            div [
                                on.afterRender (fun _ ->
                                    let e = JS.Document.QuerySelector(".test-106")
                                    e.ParentNode.RemoveChild(e) |> ignore
                                )
                            ] [text "#106 OK"]
                            |> View.Const
                            |> Doc.EmbedView
                        )
                        .Doc()
                )
                .FindBy(findByKey)
                .Found(found)
                .Length(myItems.ViewState.Map(fun s -> printfn "mapping length"; string s.Length))
                .Names(
                    myItems.ViewState.Map(fun s -> 
                        s.ToArray(fun i -> not (System.String.IsNullOrEmpty i.description))
                        |> Seq.map (fun i -> i.name)
                        |> String.concat ", "
                    )
                )
                .ListView(
                    itemsSub.View.DocSeqCached(Item.Key, fun key item ->
                        MyTemplate.template.ListViewItem()
                            .Name(item.Map(fun i -> i.name))
                            .Description(item.Map(fun i -> i.description))
                            .Doc()
                    )
                )
                .MyInput(var)
                .MyInputView(btnSub.View)
                .MyCallback(fun _ -> btnSub.Trigger())
                .ButtonExtraText(" now")
                .Checked(chk)
                .IsChecked(chk.View.Map(function true -> "checked" | false -> "not checked"))
                .NameChanged(fun e -> 
                   let key = if e.Event?which then e.Event?which else e.Event?keyCode
                   if key = 13 then newName := "")
                .PRendered(fun (el: Dom.Element) -> var := el.GetAttribute("id"))
                .ControlTests(
                    let clk = Var.Create ""
                    let chk = Var.Create true
                    let chkl = Var.Create [ 2 ]
                    let inp = Var.Create "hello"
                    let iinp = Var.Create (CheckedInput.Make 42)
                    let ri = Var.Create 0
                    [ 
                        p [] [
                            Doc.Button "Click me" [] (fun () -> clk := "Clicked!")
                            textView clk.View
                        ] :> Doc
                        p [] [
                            Doc.CheckBox [] chk 
                            textView (chk.View.Map(function false -> "Check this" | true -> "Uncheck this"))
                        ] :> Doc
                        p [] [
                            for i in 1 .. 5 ->
                                Doc.CheckBoxGroup [] i chkl :> Doc 
                            yield textView (chkl.View.Map(fun l -> "Checked indices:" + (l |> List.map string |> String.concat ", ")))
                        ] :> Doc
                        p [] [
                            Doc.Input [] inp 
                            textView (inp.View.Map(fun s -> "You said: " + s))
                        ] :> Doc
                        p [] [
                            Doc.IntInput [] iinp 
                            textView (iinp.View.Map(function Valid (i, _) -> "It's an int: " + string i | Invalid _ -> "Can't parse" | Blank _ -> "Empty" ))
                        ] :> Doc
                        p [] [
                            for i in 1 .. 5 ->
                                Doc.Radio [] i ri :> Doc 
                            yield textView (ri.View.Map(fun i -> "Checked index:" + string i))
                        ] :> Doc
                    ]
                )
                .AddDiv(fun _ -> addDiv())
                .RemoveUpdater(fun _ -> removeUpdater())
                .ReAddUpdater(fun _ -> reAddUpdater())
                .RemoveAllUpdaters(fun _ -> removeAllUpdaters())
                .IncrEltUpdaterTest(fun _ -> testCounter := !testCounter + 1)
                .EltUpdaterTest(eltUpdater)
                .Username(username)
                .Password(password)
                .Username1(username.View)
                .Submit(fun _ -> submit.Trigger())
                .NestedInstantiationTest(MyTemplate.template.L3().MIAttr(Attr.Style "color" "red").Ok("Ok").Doc())
                .Create()

        Anim.UseAnimations <- false

        doc.Vars.NewName := "Set from templateInstance.Vars"

        div [] [
            doc.Doc
            Regression67.Doc
        ]
        |> Doc.RunById "main"

        Console.Log("Running JavaScript Entry Point..")
