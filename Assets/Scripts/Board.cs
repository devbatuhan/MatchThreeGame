using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Board : MonoBehaviour {

	[SerializeField]private int width;
	[SerializeField]private int height;
	[SerializeField]private int borderSize;
	
	[SerializeField]private GameObject tilePrefab;
	[SerializeField]private GameObject[] gamePiecePrefabs;

	[SerializeField]private float swapTime = 0.5f;
	
	private Tile[,] m_allTiles;
	private GamePiece[,] m_AllGamePieces;

	private Tile clickedTile;
	private Tile targetTile;

	private bool m_playerInputEnabled = true;

	private void Start () 
	{
		m_allTiles = new Tile[width,height];
		m_AllGamePieces = new GamePiece[width,height];
		
		SetupTiles();
		SetupCamera();
		FillBoard(10, 0.5f);
	}
	private void SetupTiles() 
	{
		for (int i = 0; i < width; i++) {
			for (int j = 0; j < height; j++) {
				GameObject tile = Instantiate (tilePrefab, new Vector3(i, j, 0), Quaternion.identity) as GameObject;
				tile.name = "Tile (" + i + "," + j + ")";
				m_allTiles [i,j] = tile.GetComponent<Tile>();
				tile.transform.parent = transform;
				m_allTiles[i, j].Init(i,j, this);
			}
		}
	}
	private void SetupCamera() 
	{
		if (Camera.main is not null) 
		{
			var main = Camera.main;
			main.transform.position = new Vector3((float)(width - 1) / 2f, (float)(height - 1) / 2f, -10f);
			float aspectRatio = (float)Screen.width / (float)Screen.height;
			float verticalSize = (float)height / 2f + (float)borderSize;
			float horizontalSize = ((float)width / 2f + (float)borderSize) / aspectRatio;
			main.orthographicSize = (verticalSize > horizontalSize) ? verticalSize : horizontalSize;
		}
		else
		{
			Debug.LogWarning("YOU NEED MAIN CAMERA!");
		}
	}
	private GameObject GetRandomGamePiece()
	{
		int randomIdx = Random.Range(0, gamePiecePrefabs.Length);

		if (gamePiecePrefabs[randomIdx] == null)
		{
			Debug.LogWarning("BOARD: " + randomIdx + "does not contain a valid Game");
		}
		return gamePiecePrefabs[randomIdx];
	}
	public void PlaceGamePiece(GamePiece gamePiece, int x, int y)
	{
		if (gamePiece == null)
		{
			Debug.LogWarning("BOARD: Invalid GamePiece!");
			return;
		}
		var transformGamePiece = gamePiece.transform;
		transformGamePiece.position = new Vector2(x, y);
		transformGamePiece.rotation = Quaternion.identity;
		if (IsWithinBounds(x, y))
		{
			m_AllGamePieces[x, y] = gamePiece;
		}
		gamePiece.SetCoord(x,y);
	}
	private bool IsWithinBounds(int x, int y)
	{
		return (x >= 0 && x < width && y >= 0 && y < height);
	}
	private GamePiece FillRandomAt(int x, int y,int falseYOffset = 0, float moveTime = 0.1f)
	{
		var randomPiece = Instantiate(GetRandomGamePiece(), Vector3.zero, Quaternion.identity) as GameObject;

		if (randomPiece != null)
		{
			randomPiece.GetComponent<GamePiece>().Init(this);
			PlaceGamePiece(randomPiece.GetComponent<GamePiece>(), x, y);

			if (falseYOffset != 0)
			{
				randomPiece.transform.position = new Vector3(x, y + falseYOffset, 0);
				randomPiece.GetComponent<GamePiece>().Move(x,y,moveTime);
			}
			
			randomPiece.transform.parent = transform;
			return randomPiece.GetComponent<GamePiece>();
		}
		return null;
	}
	private void FillBoard(int falseYOffset = 0, float moveTime = 0.1f)
	{
		int maxIterations = 100;
		int iterations = 0;
		
		for (int i = 0; i < width; i++)
		{
			for (int j = 0; j < height; j++)
			{
				if (m_AllGamePieces[i, j] == null)
				{
					GamePiece piece = FillRandomAt(i, j, falseYOffset, moveTime);
					iterations = 0;
					while (HasMatchOnFill(i, j))
					{
						ClearPieceAt(i, j);
						piece = FillRandomAt(i, j, falseYOffset, moveTime);
						iterations++;

						if (iterations >= maxIterations)
						{
							Debug.Log("break ========================");
							break;
						}
					}
				}
				
			}
		}
	} 
	private bool HasMatchOnFill(int x, int y, int minLength = 3)
	{
		List<GamePiece> leftMatches = FindMatches(x, y, new Vector2(-1, 0), minLength);
		List<GamePiece> downwardMatches = FindMatches(x, y, new Vector2(0, -1), minLength);

		if (leftMatches == null)
		{
			leftMatches = new List<GamePiece>();
		}
		if (downwardMatches == null)
		{
			downwardMatches = new List<GamePiece>();
		}
		return (leftMatches.Count > 0 || downwardMatches.Count > 0);
	}
	public void ClickTile(Tile tile)
	{
		if (clickedTile == null)
		{
			clickedTile = tile;
			Debug.Log("Clicked Tile: " + tile.name);
		}
	}
	public void DragToTile(Tile tile)
	{
		if (clickedTile != null && IsNextTo(tile, clickedTile))
		{
			targetTile = tile;
		}
	}
	public void ReleaseTile()
	{
		if (clickedTile != null && targetTile != null)
		{
			SwitchTiles(clickedTile, targetTile);
		}
		
		clickedTile = null;
		targetTile = null;
	}
	private void SwitchTiles(Tile clickedTile, Tile targetTile)
	{
		StartCoroutine(SwitchTilesRoutine(clickedTile, targetTile));
	}
	private IEnumerator SwitchTilesRoutine(Tile clickedTile, Tile targetTile)
	{
		if (m_playerInputEnabled)
		{
			GamePiece clickedPiece = m_AllGamePieces[clickedTile.XIndex, clickedTile.YIndex];
			GamePiece targetPiece = m_AllGamePieces[targetTile.XIndex, targetTile.YIndex];
			if(targetPiece != null && clickedPiece != null)
			{
				clickedPiece.Move(targetTile.XIndex, targetTile.YIndex, swapTime);
				targetPiece.Move(clickedTile.XIndex, clickedTile.YIndex, swapTime);

				yield return new WaitForSeconds(swapTime);

				List<GamePiece> clickedPieceMatches = FindMatchesAt(clickedTile.XIndex, clickedTile.YIndex);
				List<GamePiece> targetPieceMatches = FindMatchesAt(targetTile.XIndex, targetTile.YIndex);

				if (targetPieceMatches.Count == 0 && clickedPieceMatches.Count == 0)
				{
					clickedPiece.Move(clickedTile.XIndex, clickedTile.YIndex, swapTime);
					targetPiece.Move(targetTile.XIndex, targetTile.YIndex, swapTime);
				}
				else
				{
					yield return new WaitForSeconds(swapTime);
					ClearAndRefillBoard(clickedPieceMatches.Union(targetPieceMatches).ToList());
				}
			}
		}
	}
	private bool IsNextTo(Tile start, Tile end)
	{
		if (Mathf.Abs(start.XIndex - end.XIndex) == 1 && start.YIndex == end.YIndex)
		{
			return true;
		}
		if (Mathf.Abs(start.YIndex - end.YIndex) == 1 && start.XIndex == end.XIndex)
		{
			return true;
		}
		return false;
	}
	private List<GamePiece> FindMatches(int startX, int startY, Vector2 searchDirection, int minLength = 3)
	{
		List<GamePiece> matches = new List<GamePiece>();
		GamePiece startPiece = null;
		if (IsWithinBounds(startX, startY))
		{
			startPiece = m_AllGamePieces[startX, startY];
		}
		if (startPiece != null)
		{
			matches.Add(startPiece);
		}
		else
		{
			return null;
		}
		int maxValue = (width > height) ? width : height;
		for (int i = 1; i < maxValue; i++)
		{
			int nextX = startX + (int)Mathf.Clamp(searchDirection.x, -1, 1) * i;
			int nextY = startY + (int)Mathf.Clamp(searchDirection.y, -1, 1) * i;
			if (!IsWithinBounds(nextX, nextY))
			{
				break;
			}
			GamePiece nextPiece = m_AllGamePieces[nextX, nextY];
			
			if (nextPiece == null)
			{
				break;
			}
			else
			{
				if (nextPiece.matchValue == startPiece.matchValue && !matches.Contains(nextPiece))
				{
					matches.Add(nextPiece);
				}
				else
				{
					break;
				}
			}
		}
		if (matches.Count >= minLength)
		{
			return matches;
		}
		return null;
	}
	private List<GamePiece> FindVerticalMatches(int startX, int startY, int minLength = 3)
	{
		List<GamePiece> upwardMatches = FindMatches(startX, startY, new Vector2(0, 1), 2);
		List<GamePiece> downwardMatches = FindMatches(startX, startY, new Vector2(0, -1), 2);

		if (upwardMatches == null)
		{
			upwardMatches = new List<GamePiece>();
		}
		if (downwardMatches == null)
		{
			downwardMatches = new List<GamePiece>();
		}

		var combinedMatches = upwardMatches.Union(downwardMatches).ToList();
		
		return (combinedMatches.Count >= minLength) ? combinedMatches : null;
		
	}
	private List<GamePiece> FindHorizontalMatches(int startX, int startY, int minLength = 3)
	{
		List<GamePiece> rightMatches = FindMatches(startX, startY, new Vector2(1, 0), 2);
		List<GamePiece> leftMatches = FindMatches(startX, startY, new Vector2(-1, 0), 2);

		if (rightMatches == null)
		{
			rightMatches = new List<GamePiece>();
		}
		if (leftMatches == null)
		{
			leftMatches = new List<GamePiece>();
		}
		
		var combinedMatches = rightMatches.Union(leftMatches).ToList();
		return (combinedMatches.Count >= minLength) ? combinedMatches : null;
	}
	private List<GamePiece> FindMatchesAt(int x, int y, int minLength = 3)
	{
		List<GamePiece> horizMatches = FindHorizontalMatches(x, y, 3);
		List<GamePiece> vertMatches = FindVerticalMatches(x, y, 3);
		if (horizMatches == null)
		{
			horizMatches = new List<GamePiece>();
		}

		if (vertMatches == null)
		{
			vertMatches = new List<GamePiece>();
		}

		var combinedMatches = horizMatches.Union(vertMatches).ToList();
		return combinedMatches;
	}
	
	private List<GamePiece> FindMatchesAt(List<GamePiece> gamePieces, int minLength = 3)
	{
		List<GamePiece> matches = new List<GamePiece>();
		foreach (GamePiece piece in gamePieces)
		{
			matches = matches.Union(FindMatchesAt(piece.XIndex, piece.YIndex, minLength)).ToList();
		}
		return matches;
	}

	private List<GamePiece> FindAllMatches()
	{
		List<GamePiece> combinedMatches = new List<GamePiece>();
		for (int i = 0; i < width; i++)
		{
			for (int j = 0; j < height; j++)
			{
				List<GamePiece> matches = FindMatchesAt(i, j);
				combinedMatches = combinedMatches.Union(matches).ToList();
			}
		}
		return combinedMatches;
	}
	
	private void HighLightTileOff(int x, int y)
	{
		SpriteRenderer spriteRenderer = m_allTiles[x,y].GetComponent<SpriteRenderer>();
		spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, 0);
	}
	private void HighLightTileOn(int x, int y, Color col)
	{
		SpriteRenderer spriteRenderer = m_allTiles[x,y].GetComponent<SpriteRenderer>();
		spriteRenderer.color = col;
	}
	private void HighlightMatchesAt(int x, int y)
	{
		HighLightTileOff(x, y);
		var combinedMatches = FindMatchesAt(x, y);
		if (combinedMatches.Count > 0)
		{
			foreach (GamePiece piece in combinedMatches)
			{
				HighLightTileOn(piece.XIndex, piece.YIndex, piece.GetComponent<SpriteRenderer>().color);
			}
		}
	}

	private void HighLightPieces(List<GamePiece> gamePieces)
	{
		foreach (GamePiece piece in gamePieces)
		{
			if (piece != null)
			{
				HighLightTileOn(piece.XIndex, piece.YIndex, piece.GetComponent<SpriteRenderer>().color);
			}
		}
	}
	private void HighLightMatches()
	{
		for (int i = 0; i < width; i++)
		{
			for (int j = 0; j < height; j++)
			{
				HighlightMatchesAt(i, j);
			}
		}
	}
	private void ClearPieceAt(int x, int y)
	{
		GamePiece pieceToClear = m_AllGamePieces[x, y];

		if (pieceToClear != null)
		{
			m_AllGamePieces[x, y] = null;
			Destroy(pieceToClear.gameObject);
		}
		HighLightTileOff(x,y);
	}
	private void ClearBoard()
	{
		for (int i = 0; i < width; i++)
		{
			for (int j = 0; j < height; j++)
			{
				ClearPieceAt(i,j);
			}
		}
	}
	private void ClearPieceAt(List<GamePiece> gamePieces)
	{
		foreach (GamePiece piece in gamePieces)
		{
			if (piece != null)
			{
				ClearPieceAt(piece.XIndex, piece.YIndex);
			}
		}
	}
	private List<GamePiece> CollapseColumn(int column, float collapseTime = 0.1f)
	{
		List<GamePiece> movingPieces = new List<GamePiece>();

		for (int i = 0; i < height - 1; i++)
		{
			if (m_AllGamePieces[column, i] == null)
			{
				for (int j = i + 1	; j < height; j++)
				{
					if (m_AllGamePieces[column, j] != null)
					{
						m_AllGamePieces[column, j].Move(column, i, collapseTime * (j - i));
						m_AllGamePieces[column, i] = m_AllGamePieces[column, j];
						m_AllGamePieces[column, i].SetCoord(column, i);

						if (!movingPieces.Contains(m_AllGamePieces[column, i]))
						{
							movingPieces.Add(m_AllGamePieces[column, i]);
						}
						m_AllGamePieces[column, j] = null;
						break;
					}
				}
			}
		}
		return movingPieces;
	}
	private List<GamePiece> CollapseColumn(List<GamePiece> gamePieces)
	{
		List<GamePiece> movingPieces = new List<GamePiece>();
		List<int> columnsToCollapse = GetColumns(gamePieces);

		foreach (int column in columnsToCollapse)
		{
			movingPieces = movingPieces.Union(CollapseColumn(column)).ToList();
		}

		return movingPieces;
	}
	private List<int> GetColumns(List<GamePiece> gamePieces)
	{
		List<int> columns = new List<int>();

		foreach (GamePiece piece in gamePieces)
		{
			if (!columns.Contains(piece.XIndex)) ;
			{
				columns.Add(piece.XIndex);
			}
		}
		return columns;
	}

	private void ClearAndRefillBoard(List<GamePiece> gamePieces)
	{
		StartCoroutine(ClearAndRefillBoardRoutine(gamePieces));
	}

	private IEnumerator ClearAndRefillBoardRoutine(List<GamePiece> gamePieces)
	{
		m_playerInputEnabled = false;
		List<GamePiece> matches = gamePieces;

		do
		{
			yield return StartCoroutine(ClearAndCollapseRoutine(matches));
			yield return null;

			yield return StartCoroutine(RefillRoutine());
			matches = FindAllMatches();
			yield return new WaitForSeconds(0.5f);
		} 
		while (matches.Count != 0);
		m_playerInputEnabled = true;


	}

	private IEnumerator RefillRoutine()
	{
		FillBoard(10, 0.5f);
		yield return null;
	}

	private IEnumerator ClearAndCollapseRoutine(List<GamePiece> gamePieces)
	{
		List<GamePiece> movingPieces = new List<GamePiece>();
		List<GamePiece> matches = new List<GamePiece>();

		HighLightPieces(gamePieces);
		
		yield return new WaitForSeconds(0.5f);
		bool isFinished = false;

		while (!isFinished)
		{
			ClearPieceAt(gamePieces);
			yield return new WaitForSeconds(0.25f);
			movingPieces = CollapseColumn(gamePieces);

			while (!isCollapsed(movingPieces))
			{
				yield return null;
			}
			yield return new WaitForSeconds(0.2f);
			matches = FindMatchesAt(movingPieces);

			if (matches.Count == 0)
			{
				isFinished = true;
				break;
			}
			else
			{
				yield return StartCoroutine(ClearAndCollapseRoutine(matches));
			}
		}
		yield return null;
	}

	private bool isCollapsed(List<GamePiece> gamePieces)
	{
		foreach (GamePiece piece in gamePieces)
		{
			if (piece != null)
			{
				if (piece.transform.position.y - (float)piece.YIndex > 0.001f)
				{
					return false;
				}
			}
		}
		return true;
	}
}
