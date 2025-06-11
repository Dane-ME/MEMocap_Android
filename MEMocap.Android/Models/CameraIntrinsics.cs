using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MEMocap.Android.Models
{
    public class CameraIntrinsics
    {
        public string CameraId { get; set; }         // ID camera
        public string CameraName { get; set; }      // Tên hiển thị

        public float FocalLengthX { get; set; }      // fx - tiêu cự theo trục X (pixel)
        public float FocalLengthY { get; set; }      // fy - tiêu cự theo trục Y (pixel)
        public float PrincipalPointX { get; set; }   // cx - tâm quang học X (pixel)
        public float PrincipalPointY { get; set; }   // cy - tâm quang học Y (pixel)
        public float SkewFactor { get; set; }        // s - hệ số skew

        public int ImageWidth { get; set; }          // độ rộng ảnh (pixel)
        public int ImageHeight { get; set; }         // độ cao ảnh (pixel)

        public float HorizontalFOV { get; set; }     // góc nhìn ngang (độ)
        public float VerticalFOV { get; set; }       // góc nhìn dọc (độ)

        // Hệ số méo ảnh
        public float RadialDistortion1 { get; set; }     // k1
        public float RadialDistortion2 { get; set; }     // k2
        public float TangentialDistortion1 { get; set; }  // p1
        public float TangentialDistortion2 { get; set; }  // p2

        // Ma trận nội tại 3x3 (K matrix)
        public float[,] GetIntrinsicMatrix()
        {
            return new float[,]
            {
            { FocalLengthX, SkewFactor, PrincipalPointX },
            { 0, FocalLengthY, PrincipalPointY },
            { 0, 0, 1 }
            };
        }
    }
}
