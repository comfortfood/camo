using UnityEngine;

public class Explorer : MonoBehaviour
{
    public Material mat;
    public Vector2 pos;
    public float scale, angle;

    private Vector2 smoothPos;
    private float smoothScale;
    private float smoothAngle;

    private void UpdateShader()
    {
        smoothPos = Vector2.Lerp(smoothPos, pos, .03f);
        smoothScale = Mathf.Lerp(smoothScale, scale, .03f);
        smoothAngle = Mathf.Lerp(smoothAngle, angle, .03f);

        // float aspect = Screen.width / (float)Screen.height;
        float scaleX = smoothScale;
        float scaleY = smoothScale;

        // if (aspect > 1)
        //     scaleY /= aspect;
        // else
        //     scaleX *= aspect;

        mat.SetVector("_Area", new Vector4(smoothPos.x, smoothPos.y, scaleX, scaleY));
        mat.SetFloat("_Angle", smoothAngle);
    }

    private void HandleInputs()
    {
        if (Input.GetKey(KeyCode.Alpha1))
            scale *= .97f;
        if (Input.GetKey(KeyCode.Alpha2))
            scale *= 1.03f;
        if (Input.GetKey(KeyCode.Q))
            angle += .01f;
        if (Input.GetKey(KeyCode.E))
            angle -= .01f;

        Vector2 dir = new Vector2(.01f * scale, 0);
        float s = Mathf.Sin(angle);
        float c = Mathf.Cos(angle);
        dir = new Vector2(dir.x * c, dir.x * s);

        if (Input.GetKey(KeyCode.A))
            pos -= dir;
        if (Input.GetKey(KeyCode.D))
            pos += dir;

        dir = new Vector2(-dir.y, dir.x);

        if (Input.GetKey(KeyCode.S))
            pos -= dir;
        if (Input.GetKey(KeyCode.W))
            pos += dir;
    }

    void FixedUpdate()
    {
        HandleInputs();
        UpdateShader();
    }
}