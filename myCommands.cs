// (C) Copyright 2023 by  
//  alamurit
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;

// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(GibTools.MyCommands))]

namespace GibTools
{
    // This class is instantiated by AutoCAD for each document when
    // a command is called by the user the first time in the context
    // of a given document. In other words, non static data in this class
    // is implicitly per-document!
    public class MyCommands
    {
        // The CommandMethod attribute can be applied to any public  member 
        // function of any public class.
        // The function should take no arguments and return nothing.
        // If the method is an intance member then the enclosing class is 
        // intantiated for each document. If the member is a static member then
        // the enclosing class is NOT intantiated.
        //
        // NOTE: CommandMethod has overloads where you can provide helpid and
        // context menu.

        //Introduction Command
        [CommandMethod("GibToolsGroup","TiAbtTool","TiAbtTool", CommandFlags.Modal)]
        public void TiAbtTool()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed;
            if (doc != null)
            {
                ed = doc.Editor;
                ed.WriteMessage("Hello user, this is GIB Tools developed by alamurit\nUse this tool to automatically generate GIBs");
            }
        }

        //GIBTool
        [CommandMethod("GibToolsGroup","TiGenerateGib","TiGenerateGib",CommandFlags.Modal)]
        public void TiGenerateGib()
        {
            //Get document and database
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            //Elbow angle
            double elbowAngle = 0;

            //Start transaction
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                //Prompt to select poly line
                PromptEntityOptions polyLineSelectionPrompt = new PromptEntityOptions("\nSelect center line representing GIB: ");
                polyLineSelectionPrompt.SetRejectMessage("\nInvalid selection!, pl select POLYLINE only.");
                polyLineSelectionPrompt.AddAllowedClass(typeof(Polyline), true);

                PromptEntityResult polyLineSelectionResult = doc.Editor.GetEntity(polyLineSelectionPrompt);

                if (polyLineSelectionResult.Status != PromptStatus.OK)
                {
                    return;
                }


                //open selected polyline for reading
                Polyline gibPolyine = tr.GetObject(polyLineSelectionResult.ObjectId, OpenMode.ForRead) as Polyline;

                //get points of that polyline
                Point3dCollection elbowLocationCollection = new Point3dCollection();    //Creates an array kind of object for collection of points
                for (int i = 0; i < gibPolyine.NumberOfVertices; i++)
                {
                    //if (i == 0)
                    //{
                    //    continue;
                    //}
                    Point3d tempElbowLocation = gibPolyine.GetPoint3dAt(i);
                    elbowLocationCollection.Add(tempElbowLocation);
                }

                //Prompt user to select block
                PromptEntityOptions blockSelectionPrompt = new PromptEntityOptions("\nSelect elbow block: ");
                blockSelectionPrompt.SetRejectMessage("\nInvalid selection. Pl select a BLOCK.");
                blockSelectionPrompt.AddAllowedClass(typeof(BlockReference), true);

                PromptEntityResult blockSelectionResult = doc.Editor.GetEntity(blockSelectionPrompt);

                if (blockSelectionResult.Status != PromptStatus.OK)
                {
                    return;
                }

                //Prompt for offset distance
                PromptDistanceOptions gibDiaPrompt = new PromptDistanceOptions("Enter GIB outer dia: \n");
                gibDiaPrompt.AllowNegative = true;
                PromptDoubleResult gibDiaResult = doc.Editor.GetDistance(gibDiaPrompt);

                if (gibDiaResult.Status != PromptStatus.OK)
                    return;

                double gibDIaValue = gibDiaResult.Value;

                //open block reference for read
                BlockReference localElbowBlockReference = tr.GetObject(blockSelectionResult.ObjectId, OpenMode.ForRead) as BlockReference;

                ////insert block at each point with orientation
                for (int j = 1; j < elbowLocationCollection.Count - 1; j++)
                {
                    Point3d previousVertex = elbowLocationCollection[j - 1];
                    Point3d currentVertex = elbowLocationCollection[j];
                    Point3d nextVertex = elbowLocationCollection[j + 1];

                    //set local block reference
                    BlockReference localEblowReference = new BlockReference(currentVertex, localElbowBlockReference.BlockTableRecord);

                    double p1x = previousVertex.X, p1y = previousVertex.Y;
                    double oX = currentVertex.X, oY = currentVertex.Y;
                    double p2x = nextVertex.X, p2y = nextVertex.Y;
                    //double subtractorX = 0, subtractorY = 0;

                    //Find the qudrant
                    p1x -= oX;
                    p2x -= oX;
                    p1y -= oY;
                    p2y -= oY;
                    //oX -= oX;
                    //oY -= oY;
                    if ((p1x > 0 && p2y > 0) || (p2x > 0 && p1y > 0))
                    {
                        elbowAngle = 0;
                    }
                    else if ((p1x < 0 && p2y > 0) || (p2x < 0 && p1y > 0))
                    {
                        elbowAngle = 90 * (Math.PI / 180);
                    }
                    else if ((p1x < 0 && p2y < 0) || (p2x < 0 && p1y < 0))
                    {
                        elbowAngle = 180 * (Math.PI / 180);
                    }
                    else if ((p1x > 0 && p2y < 0) || (p2x > 0 && p1y < 0))
                    {
                        elbowAngle = 270 * (Math.PI / 180);
                    }
                    doc.Editor.WriteMessage(elbowAngle.ToString());
                    doc.Editor.WriteMessage("\n");

                    //define direction vector
                    //Vector3d elbowDirection = nextVertex - currentVertex;

                    //set block reference properties
                    localEblowReference.ScaleFactors = localElbowBlockReference.ScaleFactors;
                    //localEblowReference.Rotation = Vector3d.ZAxis.GetAngleTo(nextVertex - currentVertex, Vector3d.ZAxis);
                    //localEblowReference.Rotation = currentVertex.za.GetAngleTo(nextVertex - currentVertex, Vector3d.ZAxis);
                    //localEblowReference.Rotation = elbowDirection.AngleOnPlane(Vector3d.ZAxis);
                    //localEblowReference.Rotation = Vector3d.XAxis.GetAngleTo(nextVertex - currentVertex);
                    //localEblowReference.Rotation = elbowDirection.GetAngleTo(nextVertex - currentVertex);
                    //double elbowAngle = Vector3d.XAxis.GetAngleTo(nextVertex - currentVertex);

                    localEblowReference.Rotation = elbowAngle;

                    //if (elbowDirection.Y < 0) // Quadrant III or IV
                    //{
                    //    elbowAngle = 2 * Math.PI - elbowAngle;
                    //}
                    //else if (elbowDirection.X < 0) // Quadrant II
                    //{
                    //    elbowAngle += Math.PI;
                    //}

                    //// Convert the angle from radians to degrees
                    //double angleDegrees = elbowAngle * (180.0 / Math.PI);

                    //localEblowReference.Rotation = angleDegrees;

                    //if(currentVertex.X<0&&)


                    //Add new block reference to drawing
                    BlockTableRecord elbowBlockTableRecord = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                    elbowBlockTableRecord.AppendEntity(localEblowReference);
                    tr.AddNewlyCreatedDBObject(localEblowReference, true);

                    //offset centerline
                    DBObjectCollection offsetResult = gibPolyine.GetOffsetCurves(gibDIaValue);
                    DBObjectCollection negOffsetResult = gibPolyine.GetOffsetCurves(-1 * gibDIaValue);
                    foreach(Entity acEntity in offsetResult)
                    {
                        //add each entity
                        elbowBlockTableRecord.AppendEntity(acEntity);
                        tr.AddNewlyCreatedDBObject(acEntity, true);
                    }
                    foreach (Entity acEntity in negOffsetResult)
                    {
                        //add each entity
                        elbowBlockTableRecord.AppendEntity(acEntity);
                        tr.AddNewlyCreatedDBObject(acEntity, true);
                    }
                }

                //commit transaction
                tr.Commit();
            }
        }

        // Modal Command with localized name
        //[CommandMethod("MyGroup", "MyCommand", "MyCommandLocal", CommandFlags.Modal)]
        //public void MyCommand() // This method can have any name
        //{
        //    // Put your command code here
        //    Document doc = Application.DocumentManager.MdiActiveDocument;
        //    Editor ed;
        //    if (doc != null)
        //    {
        //        ed = doc.Editor;
        //        ed.WriteMessage("Hello, this is your first command.");

        //    }
        //}

        // Modal Command with pickfirst selection
        //[CommandMethod("MyGroup", "MyPickFirst", "MyPickFirstLocal", CommandFlags.Modal | CommandFlags.UsePickSet)]
        //public void MyPickFirst() // This method can have any name
        //{
        //    PromptSelectionResult result = Application.DocumentManager.MdiActiveDocument.Editor.GetSelection();
        //    if (result.Status == PromptStatus.OK)
        //    {
        //        // There are selected entities
        //        // Put your command using pickfirst set code here
        //    }
        //    else
        //    {
        //        // There are no selected entities
        //        // Put your command code here
        //    }
        //}

        // Application Session Command with localized name
        //[CommandMethod("MyGroup", "MySessionCmd", "MySessionCmdLocal", CommandFlags.Modal | CommandFlags.Session)]
        //public void MySessionCmd() // This method can have any name
        //{
        //    // Put your command code here
        //}

        // LispFunction is similar to CommandMethod but it creates a lisp 
        // callable function. Many return types are supported not just string
        // or integer.
        //[LispFunction("MyLispFunction", "MyLispFunctionLocal")]
        //public int MyLispFunction(ResultBuffer args) // This method can have any name
        //{
        //    // Put your command code here

        //    // Return a value to the AutoCAD Lisp Interpreter
        //    return 1;
        //}

    }

}
