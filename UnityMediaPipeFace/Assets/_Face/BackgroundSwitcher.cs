using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BackgroundSwitcher : MonoBehaviour
{
    // Switch characters with shift
    // Switch backgrounds with tab

    public GameObject vig;
    public RawImage bgimg;
    public GameObject frame;
    public BG[] bgs;
    public GameObject[] chars;

    private int bgIndex = 0;
    private int cIndex = 0;

    [System.Serializable]
    public class BG
    {
        public Texture tex;
        public bool vig = true;
        public bool frame = true;
        public Color col = Color.white;
    }

    private void Swap()
    {
        bgIndex = (bgIndex + 1) % bgs.Length;
        bgimg.texture = bgs[bgIndex].tex;
        bgimg.color = bgs[bgIndex].col;
        vig.SetActive(bgs[bgIndex].vig);
        frame.SetActive(bgs[bgIndex].frame);
    }
    private void SwapCharacter()
    {
        chars[cIndex].SetActive(false);
        cIndex = (cIndex + 1) % chars.Length;
        chars[cIndex].SetActive(true);
        FindObjectOfType<CameraController>().lookTarget = chars[cIndex].GetComponent<AvatarFace>();
    }

    private void Start()
    {
        SwapCharacter();
        Swap();
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            Swap();
        }
        if (Input.GetKeyDown(KeyCode.LeftShift)||Input.GetKeyDown(KeyCode.RightShift))
        {
            SwapCharacter();
        }
    }
}
