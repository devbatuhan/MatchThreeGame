using UnityEngine;

public class Tile : MonoBehaviour
{
	[field: SerializeField]public int XIndex { get; set; }
	[field: SerializeField]public int YIndex { get; set; }

	private Board _board;

	public void Init(int x, int y, Board boardInit)
	{
		XIndex = x;
		YIndex = y;
		_board = boardInit;
	}
	private void OnMouseDown()
	{
		if (_board != null)
		{
			_board.ClickTile(this);
		}
	}
	private void OnMouseEnter()
	{
		if (_board != null)
		{
			_board.DragToTile(this);
		}
	}
	private void OnMouseUp()
	{
		if (_board != null)
		{
			_board.ReleaseTile();
		}
	}
}
