using UnityEngine;

public static class RawInputs
{
    public static float[] Get(float[] rawValues, GameObject mockGyroscope)
    {
        float pitch;
        float yaw;
        float roll;
        if (mockGyroscope == null)
        {
            var q1 = Input.gyro.attitude;

            var g = new GameObject();
            var t = g.transform;
            t.localRotation = new Quaternion(q1[2], q1[0], q1[3], q1[1]);

            yaw = t.localEulerAngles[1];
            t.Rotate(0, yaw, 0);

            pitch = t.localEulerAngles[0];
            t.Rotate(pitch, 0, 0);

            roll = t.localEulerAngles[2];
            t.Rotate(0, 0, roll * -1);

            Object.DestroyImmediate(g);
        }
        else
        {
            var position = mockGyroscope.transform.position;
            pitch = Mathf.Abs(position.x * 36) % 360;
            yaw = Mathf.Abs(position.y * 36) % 360;
            roll = Mathf.Abs(position.z * 36) % 360;
        }

        if (pitch > 180)
        {
            pitch -= 360;
        }

        yaw /= 57.2958f * Mathf.PI * 2;
        pitch = (pitch / -57.2958f + Mathf.PI / 2) / Mathf.PI;
        roll /= 57.2958f * Mathf.PI * 2;

        var arr = new float[21];

        if (rawValues is not { Length: 21 })
        {
            arr = new[]
            {
                pitch,
                roll,
                yaw,
                pitch,
                roll,
                yaw,
                pitch,
                roll,
                yaw,
                pitch,
                roll,
                yaw,
                pitch,
                roll,
                yaw,
                pitch,
                roll,
                yaw,
                pitch,
                roll,
                yaw
            };
        }
        else
        {
            for (var i = 0; i < 18; i++)
            {
                arr[i] = rawValues[i];
            }

            arr[18] = pitch;
            arr[19] = roll;
            arr[20] = yaw;

            for (var i = 0; i < 3; i++)
            {
                var c = Mathf.Abs(arr[18 + i] - rawValues[18 + i]);

                if (!(c > 0.003) || !(c < 0.997)) continue;

                if ((c < 0.5 && arr[18 + i] > rawValues[18 + i]) ||
                    (c >= 0.5 && arr[18 + i] <= rawValues[18 + i]))
                {
                    arr[0 + i] += c;
                    arr[3 + i] += c;
                    arr[9 + i] += c / 2;
                    arr[12 + i] += c / 2;
                }
                else
                {
                    arr[0 + i] += 1 - c;
                    arr[6 + i] += 1 - c;
                    arr[9 + i] += 1 - c / 2;
                    arr[15 + i] += 1 - c / 2;
                }
            }

            for (var i = 0; i < 18; i++)
            {
                arr[i] %= 1.0f;
            }
        }

        return arr;
    }
}