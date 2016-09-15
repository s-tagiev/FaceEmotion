using Microsoft.ProjectOxford.Emotion.Contract;
using Microsoft.ProjectOxford.Face.Contract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Face
{
    public class FFrame
    {
        public string FileName { get; set; }
        public FaceFrame FaceFrames { get; set; }
        public Emotion[] Emotions { get; set; }
    }

    public class FaceFrame
    {
        public Microsoft.ProjectOxford.Face.Contract.Face Face { get; set; }
        public IdentifyResult Result { get; set; }
    }
}
