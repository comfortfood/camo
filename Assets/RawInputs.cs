using UnityEngine;

public static class RawInputs
{
    public static float[] Get(float[] rawInputs, GameObject mockGyroscope)
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
            t.Rotate(0f, yaw, 0f);

            pitch = t.localEulerAngles[0];
            t.Rotate(pitch, 0f, 0f);

            roll = t.localEulerAngles[2];
            t.Rotate(0f, 0f, roll * -1);

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

        yaw /= 57.2958F * Mathf.PI * 2;
        pitch = (pitch / -57.2958F + Mathf.PI / 2) / Mathf.PI;
        roll /= 57.2958F * Mathf.PI * 2;

        float pitchSmooth;
        float pitchPlus;
        float pitchMinus;
        float rollSmooth;
        float rollPlus;
        float rollMinus;
        float yawSmooth;
        float yawPlus;
        float yawMinus;

        if (rawInputs == null)
        {
            pitchSmooth = pitch;
            pitchPlus = pitch;
            pitchMinus = pitch;
            rollSmooth = roll;
            rollPlus = roll;
            rollMinus = roll;
            yawSmooth = yaw;
            yawPlus = yaw;
            yawMinus = yaw;
        }
        else
        {
            // pitch change
            var pc = Mathf.Abs(pitch - rawInputs[9]);

            if (pc > 0.003 && pc < 0.997)
            {
                if ((pc < 0.5 && pitch > rawInputs[9]) ||
                    (pc >= 0.5 && pitch <= rawInputs[9]))
                {
                    pitchSmooth = (rawInputs[0] + pc) % 1.0F;
                    pitchPlus = (rawInputs[3] + pc) % 1.0F;
                    pitchMinus = rawInputs[4];
                }
                else
                {
                    pitchSmooth = (rawInputs[0] + 1 - pc) % 1.0F;
                    pitchPlus = rawInputs[3];
                    pitchMinus = (rawInputs[4] + 1 - pc) % 1.0F;
                }
            }
            else
            {
                pitchSmooth = rawInputs[0];
                pitchPlus = rawInputs[3];
                pitchMinus = rawInputs[4];
            }

            // roll change
            var rc = Mathf.Abs(roll - rawInputs[10]);

            if (rc > 0.003 && rc < 0.997)
            {
                if ((rc < 0.5 && roll > rawInputs[10]) ||
                    (rc >= 0.5 && roll <= rawInputs[10]))
                {
                    rollSmooth = (rawInputs[1] + rc) % 1.0F;
                    rollPlus = (rawInputs[5] + rc) % 1.0F;
                    rollMinus = rawInputs[6];
                }
                else
                {
                    rollSmooth = (rawInputs[1] + 1 - rc) % 1.0F;
                    rollPlus = rawInputs[5];
                    rollMinus = (rawInputs[6] + 1 - rc) % 1.0F;
                }
            }
            else
            {
                rollSmooth = rawInputs[1];
                rollPlus = rawInputs[5];
                rollMinus = rawInputs[6];
            }

            // yaw change
            var yc = Mathf.Abs(yaw - rawInputs[11]);

            if (yc > 0.003 && yc < 0.997)
            {
                if ((yc < 0.5 && yaw > rawInputs[11]) ||
                    (yc >= 0.5 && yaw <= rawInputs[11]))
                {
                    yawSmooth = (rawInputs[2] + yc) % 1.0F;
                    yawPlus = (rawInputs[7] + yc) % 1.0F;
                    yawMinus = rawInputs[8];
                }
                else
                {
                    yawSmooth = (rawInputs[2] + 1 - yc) % 1.0F;
                    yawPlus = rawInputs[7];
                    yawMinus = (rawInputs[8] + 1 - yc) % 1.0F;
                }
            }
            else
            {
                yawSmooth = rawInputs[2];
                yawPlus = rawInputs[7];
                yawMinus = rawInputs[8];
            }
        }

        return new[]
        {
            pitchSmooth,
            rollSmooth,
            yawSmooth,
            pitchPlus,
            pitchMinus,
            rollPlus,
            rollMinus,
            yawPlus,
            yawMinus,
            pitch,
            roll,
            yaw,
        };
    }
}