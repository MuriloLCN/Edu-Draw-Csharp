using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SimpleDrawProject
{
    class SimulationData
    {
        /*
        This class is used to store all data related to a state of the simulation under one instance
        */
        public enum DRAW_MODE { TOP_LEFT, CENTER };
        public enum TRANSFORMATIONS { ROTATE, TRANSLATE, SCALE };

        // Used to reverse the transformations if needed in mousePos()
        public List<TRANSFORMATIONS> operations = new List<TRANSFORMATIONS>();
        public List<dynamic> operationParameters = new List<dynamic>();

        public DRAW_MODE currentRectMode = DRAW_MODE.TOP_LEFT;
        public DRAW_MODE currentCircleMode = DRAW_MODE.CENTER;

        public Point translationCoefficients = new Point(0, 0);
        public float[] scalingCoefficients = { 1, 1 };

        public bool useAbsolutePosition = false;
        public bool accountForTransformations = false;

        public Color currentStrokeColor = Color.Black;
        public Color currentFillColor = Color.Black;

        public Color? backgroundColor = Color.White;

        public bool fillState = true;
        public bool strokeState = true;

        public float rotationAngle = 0.0F;

        public Font textFont = new Font("Times New Roman", 12.0f);

        public SolidBrush fillBrush = new SolidBrush(Color.Black);
        public Pen strokePen = new Pen(new SolidBrush(Color.Black), 1);

        public int currentStrokeWeight = 1;

        private float radians(float angle)
        {
            /*
            Converts degrees to radians
            */
            return (float)((angle / 180) * Math.PI);
        }

        private void revertTranslate(ref PointF p, int dx, int dy)
        {
            /*
            Reverts a translation operation on a coordinate
            */
            p.X -= dx;
            p.Y -= dy;
        }

        private void revertRotate(ref PointF p, float angle)
        {
            /*
            Reverts a rotation transformation o a coordinate
            */
            float angleRad = radians(-angle);
            double sin = Math.Sin(angleRad);
            double cos = Math.Cos(angleRad);
            float oldx = p.X;
            float oldy = p.Y;

            p.X = (float)(oldx * cos - oldy * sin);
            p.Y = (float)(oldx * sin + oldy * cos);
        }

        private void revertScale(ref PointF p, float sx, float sy)
        {
            /*
            Reverts a scaling operation on a coordinate
            */
            p.X /= sx;
            p.Y /= sy;
        }

        public void resetTransformations()
        {
            /*
            Resets all variables related to transformations to their default state
            */
            translationCoefficients = new Point();
            rotationAngle = 0;
            scalingCoefficients[0] = 1;
            scalingCoefficients[1] = 1;
            
            clearTfLists();
        }

        private void removeTranformationType(TRANSFORMATIONS type)
        {
            /*
            Removes a certain transformation type from the operations list and the appropriate associated
            parameter from operationParameters list
            */
            List<TRANSFORMATIONS> newOperations = new List<TRANSFORMATIONS>();
            List<dynamic> newOperationParameters = new List<dynamic>();

            for (int i = 0; i < operations.Count; i++)
            {
                if (operations[i] != type)
                {
                    newOperations.Add(operations[i]);
                    newOperationParameters.Add(operationParameters[i]);
                }
            }

            operations = newOperations;
            operationParameters = newOperationParameters;
        }

        public void resetRotation()
        {
            /*
            Resets variables related to rotations
            */
            removeTranformationType(TRANSFORMATIONS.ROTATE);
            rotationAngle = 0;
        }

        public void resetTranslation()
        {
            /*
            Resets variables related to translations
            */
            removeTranformationType(TRANSFORMATIONS.TRANSLATE);
            translationCoefficients = new Point(0, 0);
        }

        public void resetScaling()
        {
            /*
            Resets variables related to scaling
            */
            removeTranformationType(TRANSFORMATIONS.SCALE);
            scalingCoefficients[0] = 1;
            scalingCoefficients[1] = 1;
        }

        public void clearTfLists()
        {
            /*
            Clears the applied transformations lists
            */
            operations = new List<TRANSFORMATIONS>();
            operationParameters = new List<dynamic>();
        }

        public Point getOriginalPoint(Point p)
        {
            /*
            Reverts a coordinate through all applied transformations
            */
            List<TRANSFORMATIONS> usedOperations;
            List<dynamic> usedParams;

            usedOperations = operations;
            usedParams = operationParameters;            

            PointF current = (PointF)p;
            for (int i = 0; i < usedOperations.Count; i++)
            {
                if (usedOperations[i] == TRANSFORMATIONS.ROTATE)
                {
                    revertRotate(ref current, (float)usedParams[i]);
                }
                if (usedOperations[i] == TRANSFORMATIONS.SCALE)
                {
                    revertScale(ref current, (float)usedParams[i].X, usedParams[i].Y);
                }
                if (usedOperations[i] == TRANSFORMATIONS.TRANSLATE)
                {
                    revertTranslate(ref current, usedParams[i].X, usedParams[i].Y);
                }
            }
            Point final = new Point();
            final.X = (int)Math.Round(current.X);
            final.Y = (int)Math.Round(current.Y);

            return final;
        }
    }
    public class SimpleDraw
    {
        // PictureBox element to be drawn on
        public PictureBox canvas = null;

        public int frameCount = 0;
        private int deltaTime = 16;

        public Bitmap currentFrame = null;
        public Graphics graphics;

        private bool antiAlias = false;

        public int width = 0;
        public int height = 0;

        private bool isPaused = false;
        private bool quitted = false;

        public bool resetAfterLoop = true;

        private SimulationData data = new SimulationData();
        private Stack<SimulationData> dataStack = new Stack<SimulationData>();
        
        private SimulationData getData()
        {
            /*
            Retrieves the adequate SimulationData instance to act upon
            */
            if (dataStack.Count == 0)
            {
                return data;
            }
            else
            {
                return dataStack.Peek();
            }
        }

        private void reapply_transformations()
        {
            /*
            Reapplies all transformations onto the graphics
            */
            SimulationData currentData = getData();

            for (int i = 0; i < currentData.operations.Count; i++)
            {
                switch (currentData.operations[i])
                {
                    case SimulationData.TRANSFORMATIONS.ROTATE:
                        graphics.RotateTransform(currentData.operationParameters[i]);
                        // currentData.rotationAngle = currentData.operationParameters[i];
                        break;

                    case SimulationData.TRANSFORMATIONS.SCALE:
                        // currentData.scalingCoefficients[0] = currentData.operationParameters[i][0];
                        // currentData.scalingCoefficients[1] = currentData.operationParameters[i][1];

                        graphics.ScaleTransform(currentData.operationParameters[i][0], currentData.operationParameters[i][1]);
                        break;

                    case SimulationData.TRANSFORMATIONS.TRANSLATE:
                        int x = currentData.operationParameters[i].X;
                        int y = currentData.operationParameters[i].Y;

                        graphics.TranslateTransform(x, y);
                        break;
                }
            }

        }
        
        public void rectMode(string mode)
        {
            /*
            Changes the mode used to draw rectangles.
            If mode is "CENTER", center mode will be used, otherwise, the default "TOP_LEFT" mode will be used.
            */

            SimulationData currentData = getData();
            SimulationData.DRAW_MODE newMode;

            if (mode.Equals("CENTER"))
            {
                newMode = SimulationData.DRAW_MODE.CENTER;
            }
            else
            {
                newMode = SimulationData.DRAW_MODE.TOP_LEFT;
            }

            currentData.currentRectMode = newMode;
        }

        public void circleMode(string mode)
        {
            /*
            Changes the mode used to draw circles and ellipses.
            If mode is "TOP_LEFT", top left mode will be used, otherwise, the default "CENTER" mode will be used.
            */
            SimulationData currentData = getData();
            SimulationData.DRAW_MODE newMode;

            if (mode.Equals("TOP_LEFT"))
            {
                newMode = SimulationData.DRAW_MODE.TOP_LEFT;
            }
            else
            {
                newMode = SimulationData.DRAW_MODE.CENTER;
            }

            currentData.currentCircleMode = newMode;
        }

        private void getPenAndBrush(out Pen p, out SolidBrush b)
        {
            /*
            Retrieves the adequate pen and solidbrush to use on drawing
            */
            SimulationData currentData = getData();

            p = currentData.strokePen;
            b = currentData.fillBrush;
        }

        private void getCircleOffset(out int kx, out int ky, int w, int h)
        {
            /*
            Retrieves the offset to be used when drawing circles based on circle mode
            */
            SimulationData currentData = getData();

            if (currentData.currentCircleMode == SimulationData.DRAW_MODE.CENTER)
            {
                kx = -w / 2; 
                ky = -h/2;
                return;
            }

            kx = 0;
            ky = 0;
        }

        private void getRectOffset(out int kx, out int ky, int w, int h)
        {
            /*
            Retrieves the offset to be used when drawing rectangles based on rect mode
            */
            SimulationData currentData = getData();

            if (currentData.currentRectMode== SimulationData.DRAW_MODE.CENTER)
            {
                kx = -w / 2; 
                ky = -h/2;
                return;
            }

            kx = 0;
            ky = 0;
        }

        private void resetVariables()
        {
            // Resets all drawing variables to default state
            data = new SimulationData();

            dataStack = new Stack<SimulationData>();

            resetTransformations();
        }

        public void resetTransformations()
        {
            // Resets all matrix transformations in current state
            SimulationData currentData = getData();

            currentData.resetTransformations();
  
            graphics.ResetTransform();
        }

        public void scale(float scaleX, float scaleY)
        {
            // Sets the scaling of the screen to a desired size
            SimulationData currentData = getData();

            currentData.scalingCoefficients[0] *= scaleX;
            currentData.scalingCoefficients[1] *= scaleY;

            PointF temp = new PointF(scaleX, scaleY);

            currentData.operations.Add(SimulationData.TRANSFORMATIONS.SCALE);
            currentData.operationParameters.Add(temp);
            
            graphics.ScaleTransform(scaleX, scaleY);
        }

        public void zoom(float zoomFactorCurrent)
        {
            // Sets scale for both axis to a desired value
            scale(zoomFactorCurrent, zoomFactorCurrent);
        }

        public void resetTranslation()
        {
            /*
            Removes all applied translation transformations.
            */
            SimulationData data = getData();
            data.resetTranslation();
            graphics.ResetTransform();
            reapply_transformations();
        }

        public void resetRotation()
        {
            /*
            Removes all applied rotation transformations.
            */
            SimulationData data = getData();
            data.resetRotation();
            graphics.ResetTransform();
            reapply_transformations();
        }

        public void resetScaling()
        {
            /*
            Removes all applied scaling transformations.
            */
            SimulationData data = getData();
            data.resetScaling();
            graphics.ResetTransform();
            reapply_transformations();
        }

        public void point(int x, int y)
        {
            // Draws a point at coordinates (x,y) with the current stroke color
            Pen st;

            SimulationData currentData = getData();
            st = currentData.strokePen;

            graphics.DrawEllipse(st, x, y, 1, 1);
        }

        public void text(string s, int x, int y)
        {
            // Writes a string of text at the coordinates (x,y) with the current fill color

            Font usedFont;
            SolidBrush b;

            SimulationData currentData = getData();

            usedFont = currentData.textFont;
            b = currentData.fillBrush;

            // Offsets change based on drawMode
            int kx = 0;
            int ky = 0;
            SizeF stringSize = graphics.MeasureString(s, usedFont);

            getRectOffset(out kx, out ky, (int)stringSize.Width, (int)stringSize.Height);

            graphics.DrawString(s, currentData.textFont, b, new Point(x + kx, y + ky));
        }

        public void clear()
        {
            // Clears the canvas to just the background color
            Color? current;

            SimulationData currentData = getData();

            current = currentData.backgroundColor;

            if (current != null)
            {
                graphics.Clear((Color)current);
            }
            else
            {
                currentFrame.Dispose();
                graphics.Dispose();
                currentFrame = new Bitmap(width, height);
                graphics = Graphics.FromImage(currentFrame);
                GC.Collect();
            }
        }

        public void background(Color? c)
        {
            // Changes the background color and draws it (i.e, clears the screen)
            SimulationData currentData = getData();

            currentData.backgroundColor = c;

            clear();
            GC.Collect();
        }

        public void fill(Color c)
        {
            // Changes the fill state to true and sets the fill color
            // While the fill state is true, all closed figures (squares, circles, etc) will have their areas filled in with this color

            SimulationData currentData = getData();

            currentData.fillState = true;
            currentData.currentFillColor = c;
            currentData.fillBrush = new SolidBrush(c);
        }

        public void changeFont(string name, float size)
        {
            // Changes the font used for text
            SimulationData currentData = getData();

            currentData.textFont = new Font(name, size);
        }

        public void rotate(float angle)
        {
            // Rotates by a given angle
            SimulationData currentData = getData();

            graphics.RotateTransform(angle);
            currentData.rotationAngle += angle;

            currentData.operations.Add(SimulationData.TRANSFORMATIONS.ROTATE);
            currentData.operationParameters.Add(angle);
        }

        public void noFill()
        {
            // Sets the fill state to false
            // All figures drawn while fill state is false will only have their perimeters drawn
            getData().fillState = false;
        }

        public void translate(int x, int y)
        {
            // Changes the origin point of the coordinate system

            SimulationData currentData = getData();

            currentData.translationCoefficients.X += x;
            currentData.translationCoefficients.Y += y;

            Point opCoef = new Point(x, y);

            currentData.operations.Add(SimulationData.TRANSFORMATIONS.TRANSLATE);
            currentData.operationParameters.Add(opCoef);
            
            graphics.TranslateTransform(x, y);
        }

        public void strokeWeight(int s)
        {
            // Changes the stroke weight of the pen
            // Bigger stroke weight means bigger edges on figures drawn

            SimulationData currentData = getData();

            currentData.strokePen = new Pen(currentData.currentStrokeColor, s);
            currentData.currentStrokeWeight = s;
        }

        public void stroke(Color c)
        {
            // sets the stroke state to true and sets the stroking color
            // Figures drawn while stroke state is true will have their perimeters drawn to the screen
            SimulationData currentData = getData();

            currentData.strokeState = true;

            currentData.currentStrokeColor = c;
            currentData.strokePen = new Pen(new SolidBrush(c), currentData.currentStrokeWeight);
            
        }

        public void noStroke()
        {
            // Sets the stroke state to false
            // Figures drawn while stroke state is false will not have their perimeters drawn to the screen
            getData().strokeState = false;
        }

        public void frameRate(int frames)
        {
            // Sets the frame rate for the draw() function
            // Note: This must be done in setup()
            deltaTime = (int)Math.Floor((double)1000 / frames);
            if (deltaTime <= 0)
            {
                deltaTime = 1;
            }
        }

        public void circle(int x, int y, int r)
        {
            // Draws a circle at position (x,y) with radius r
            SimulationData currentData = getData();

            getPenAndBrush(out Pen p, out SolidBrush sb);
            getCircleOffset(out int kx, out int ky, 2 * r, 2 * r);

            if (currentData.fillState)
            {
                graphics.FillEllipse(sb, x + kx, y + ky, 2 * r, 2 * r);
            }
            if (currentData.strokeState)
            {
                graphics.DrawEllipse(p, x + kx, y + ky, 2 * r, 2 * r);
            }
        }

        public void ellipse(int x, int y, int w, int h)
        {
            // Draws an ellipse at (x,y) delimited by a rect with sides (w,h)
            SimulationData currentData = getData();

            getPenAndBrush(out Pen p, out SolidBrush sb);
            getCircleOffset(out int kx, out int ky, w, h);

            if (currentData.fillState)
            {
                graphics.FillEllipse(sb, x + kx, y + ky, w, h);
            }
            if (currentData.strokeState)
            {
                graphics.DrawEllipse(p, x + kx, y + ky, w, h);
            }
        }

        public void line(int x1, int y1, int x2, int y2)
        {
            // Draws a line between two points (x1,y1) and (x2,y2)
            SimulationData currentData = getData();
            Pen p;

            p = currentData.strokePen;


            if (currentData.strokeState)
                graphics.DrawLine(p, x1, y1, x2, y2);
        }

        public void rect(int x, int y, int w, int h)
        {
            // Draws a rectangle at the position (x,y) with sides (w,h)
            SimulationData currentData = getData();

            getPenAndBrush(out Pen p, out SolidBrush sb);
            getRectOffset(out int kx, out int ky, w, h);

            if (currentData.fillState)
            {
                graphics.FillRectangle(sb, x + kx, y + ky, w, h);
            }
            if (currentData.strokeState)
            {
                graphics.DrawRectangle(p, x + kx, y + ky, w, h);
            }
        }

        public void square(int x, int y, int s)
        {
            // Draws a square at position (x,y) with sides s
            SimulationData currentData = getData();

            getPenAndBrush(out Pen p, out SolidBrush sb);

            getRectOffset(out int kx, out int ky, s, s);

            if (currentData.fillState)
            {
                graphics.FillRectangle(sb, x + kx, y + ky, s, s);
            }
            if (currentData.strokeState)
            {
                graphics.DrawRectangle(p, x + kx, y + ky, s, s);
            }
        }

        public void triangle(int x1, int y1, int x2, int y2, int x3, int y3)
        {
            // Draws a triangle from three vertices (x1,y1),(x2,y2) and (x3,y3)
            SimulationData currentData = getData();
            getPenAndBrush(out Pen p, out SolidBrush sb);

            Point[] matrix = { new Point(x1, y1), new Point(x2, y2), new Point(x3, y3) };
            if (currentData.fillState)
            {
                graphics.FillPolygon(sb, matrix);
            }
            if (currentData.strokeState)
            {
                graphics.DrawPolygon(p, matrix);
            }
        }

        public void polygon(Point[] points)
        {
            // Draws a polygon from a points matrix
            SimulationData currentData = getData();
            getPenAndBrush(out Pen p, out SolidBrush sb);

            if (currentData.fillState)
            {
                graphics.FillPolygon(sb, points);
            }
            if (currentData.strokeState)
            {
                graphics.DrawPolygon(p, points);
            }
        }

        public void image(Image img, int x, int y)
        {
            // Draws an image to the screen at position x,y
            if (img == null)
                return;
            
            getRectOffset(out int kx, out int ky, img.Width, img.Height);

            graphics.DrawImage(img, new Point(x + kx, y + ky));
        }

        public void image(Image img, int x, int y, int w, int h)
        {
            // Draws an image to the screen at position x,y with sizes w,h
            if (img == null)
                return;

            getRectOffset(out int kx, out int ky, w, h);

            graphics.DrawImage(img, new Rectangle(x + kx, y + ky, w, h));
        }

        public void save(string filename)
        {
            if (filename.Equals(""))
                filename = frameCount.ToString() + ".PNG";
            
            currentFrame.Save(filename);
        }

        public void toggleAntiAlias()
        {
            // Toggles anti aliasing on or off
            antiAlias = !antiAlias;
        }

        public void push()
        {
            // Sets the temporary state to true
            // All changes to colors done while temp state is on will be reverted back upon leaving

            SimulationData newData = new SimulationData();
            SimulationData currentData = getData();

            newData.backgroundColor = currentData.backgroundColor;
            newData.fillBrush = currentData.fillBrush;
            newData.currentFillColor = currentData.currentFillColor;
            newData.currentStrokeColor = currentData.currentStrokeColor;
            newData.strokePen = currentData.strokePen;
            newData.currentCircleMode = currentData.currentCircleMode;
            newData.currentRectMode = currentData.currentRectMode;
            newData.textFont = currentData.textFont;

            newData.operationParameters = new List<dynamic>();
            newData.operations = new List<SimulationData.TRANSFORMATIONS>();

            foreach (SimulationData.TRANSFORMATIONS operation in currentData.operations)
            {
                newData.operations.Add(operation);
            }

            foreach (var parameter in currentData.operationParameters)
            {
                newData.operationParameters.Add(parameter);
            }

            newData.rotationAngle = currentData.rotationAngle;

            newData.scalingCoefficients[0] = currentData.scalingCoefficients[0];
            newData.scalingCoefficients[1] = currentData.scalingCoefficients[1];

            newData.translationCoefficients.X = currentData.translationCoefficients.X;
            newData.translationCoefficients.Y = currentData.translationCoefficients.Y;

            dataStack.Push(newData);
        }

        public void pop()
        {
            // Leaves temp state and reverts back color changes made while it was on
            // SimulationData oldData = dataStack.Pop();
            dataStack.Pop();
            // SimulationData currentData = getData();

            graphics.ResetTransform();
            reapply_transformations();
        }

        public void setAccountForTransformations(bool account)
        {
            /*
            Changes whether mousePos should account for transformations or not.
            */
            getData().accountForTransformations = account;
        }

        public void setUseAbsolutePosition(bool setUse)
        {
            getData().useAbsolutePosition = setUse;
        }

        public void pause()
        {
            // Pauses the simulation
            isPaused = true;
        }

        public void unpause()
        {
            // Resumes the simulation
            isPaused = false;
        }

        public void quit()
        {
            // Quits the simulation
            quitted = true;
        }

        private Point getOriginalPoint(Point p)
        {
            return getData().getOriginalPoint(p);
        }
        
        public Point mousePos(Form f)
        {
            // Gets mouse position in the screen

            if (f == null)
                return new Point(0, 0);

            Point absolutePos = System.Windows.Forms.Cursor.Position;

            SimulationData currentData = getData();

            if (currentData.useAbsolutePosition)
            {
                return absolutePos;
            }

            Point relativeForm = f.PointToClient(absolutePos);

            if (canvas != null)
            {
                relativeForm.X -= canvas.Location.X;
                relativeForm.Y -= canvas.Location.Y;
            }

            if (currentData.accountForTransformations)
            {
                relativeForm = getOriginalPoint(relativeForm);
            }
            
            return relativeForm;
        }

        private Action drawAct;

        private void TimerTick(object sender, EventArgs e)
        {
            // Behind the scenes of draw()
            if (isPaused)
            {
                return;
            }

            if (quitted)
            {
                drawEvent.Stop();
                GC.Collect();
                return;
            }

            getData().clearTfLists();

            if (resetAfterLoop || frameCount == 0)
            {
                currentFrame = new Bitmap(width, height);
                graphics = Graphics.FromImage(currentFrame);
            }

            if (antiAlias)
            {
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            }
            else
            {
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
            }

            frameCount++;

            drawAct();

            if (canvas != null)
            {
                canvas.Image = (Image)currentFrame;
            }

            GC.Collect();
            if (resetAfterLoop)
            {
                resetVariables();
            }
            
        }

        Timer drawEvent = new Timer();

        void protoDraw(Action draw)
        {
            drawAct = draw;
            drawEvent.Tick += new EventHandler(TimerTick);
            drawEvent.Interval = deltaTime;
            drawEvent.Start();
        }

        void protoSetup(Action setup)
        {
            if (canvas != null)
                canvas.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            setup();
        }

        public void start(Action setup, Action draw, PictureBox img)
        {
            // Start simulation with PictureBox element
            canvas = img;
            if (canvas != null)
            {
                width = img.Width;
                height = img.Height;
            }
            protoSetup(setup);
            protoDraw(draw);
        }

        public void start(Action setup, Action draw, int w, int h)
        {
            // Start simulation without canvas but with defined size
            width = w;
            height = h;

            protoSetup(setup);
            protoDraw(draw);
        }
    }
}
