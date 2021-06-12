using System;
using ModunGames.Enums;
using UnityEngine;


// ReSharper disable once CheckNamespace
namespace ModunGames {
    public static class VectorExtensions {


        /// <summary>
        /// Restricts a <see cref="Vector2"/> between a minimum and maximum value.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static Vector2 Clamp(this Vector2 value, Vector2 min, Vector2 max) {


            Vector2 clampedVector = new Vector2();


            // Clamp x value.
            if (value.x >= min.x && value.x <= max.x) clampedVector.x = value.x;
            if (value.x < min.x) clampedVector.x = min.x;
            if (value.x > max.x) clampedVector.x = max.x;


            // Clamp y value.
            if (value.y >= min.y && value.y <= max.y) clampedVector.y = value.y;
            if (value.y < min.y) clampedVector.y = min.y;
            if (value.y > max.y) clampedVector.y = max.y;


            return clampedVector;


        }
        

        /// <summary>
        /// Restricts a <see cref="Vector3"/> between a minimum and maximum value.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static Vector3 Clamp(this Vector3 value, Vector3 min, Vector3 max) {


            Vector3 clampedVector = new Vector3();


            // Clamp x value.
            if (value.x >= min.x && value.x <= max.x) clampedVector.x = value.x;
            if (value.x < min.x) clampedVector.x = min.x;
            if (value.x > max.x) clampedVector.x = max.x;


            // Clamp y value.
            if (value.y >= min.y && value.y <= max.y) clampedVector.y = value.y;
            if (value.y < min.y) clampedVector.y = min.y;
            if (value.y > max.y) clampedVector.y = max.y;


            // Clamp z value.
            if (value.z >= min.z && value.z <= max.z) clampedVector.z = value.z;
            if (value.z < min.z) clampedVector.z = min.z;
            if (value.z > max.z) clampedVector.z = max.z;


            return clampedVector;


        }


        /// <summary>
        /// Determines if the current vector position is within the rectangular bounds specified.
        /// </summary>
        /// <param name="position">The positional vector for which to check if it is within bounds.</param>
        /// <param name="lowerBound">The bottom left bound of the rectangle.</param>
        /// <param name="upperBound">The upper right bound of the rectangle.</param>
        /// <returns></returns>
        public static bool IsWithinBounds(this Vector2 position, Vector2 lowerBound, Vector2 upperBound) {


            if (lowerBound.x > upperBound.x || lowerBound.y > upperBound.y) 
                throw new ArgumentException("Lower bound exceeds upper bound!");


            return position.x >= lowerBound.x && position.x <= upperBound.x &&
                   position.y >= lowerBound.y && position.y <= upperBound.y;
            

        }


        /// <summary>
        /// Rounds the x and y values of the <see cref="Vector2"/>.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Vector2 Round(this Vector2 value) {
            return new Vector2 {
                x = Mathf.Round(value.x),
                y = Mathf.Round(value.y)
            };
        }


        /// <summary>
        /// Rounds the x and y values of the <see cref="Vector2"/>.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="invert"></param>
        /// <returns></returns>
        public static Vector2 Invert(this Vector2 value, VectorInvertOptions invert = VectorInvertOptions.XY) {
            return new Vector2 {
                x = invert == VectorInvertOptions.X || invert == VectorInvertOptions.XY ? -value.x : value.x,
                y = invert == VectorInvertOptions.Y || invert == VectorInvertOptions.XY ? -value.y : value.y
            };
        }


    }
}
