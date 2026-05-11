namespace DefaultNamespace
{
    public class NoTextureJigsawSlot : JigsawSlot
    {
        public override void Show()
        {
            base.Show();
            
            texture.SetActive(false);
        }
    }
}