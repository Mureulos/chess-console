using board;
using chess_console.Exceptions;

namespace chess;

public class ChessMatch
{
    public Board board { get; private set; }
    public int turn { get; private set; }
    public Color actualPlayerColor { get; private set; }
    
    public bool check { get; private set; }
    public bool completed { get; private set; }
    private HashSet<Piece> _pieces;
    private HashSet<Piece> _capturedPieces;
    
    public ChessMatch()
    {
        board = new Board();
        turn = 1;
        actualPlayerColor = Color.White;
        completed = false;
        _pieces = new HashSet<Piece>();
        _capturedPieces = new HashSet<Piece>();
        SetBoard();
    }

    public Piece ExecuteMoviment(Position origin, Position target)
    {
        Piece piece = board.RemovePiece(origin);
        piece.AddMove();
        Piece caughtPiece = board.RemovePiece(target);
        board.PutPiece(piece, target);
        
        if (caughtPiece != null)
            _capturedPieces.Add(caughtPiece);

        return caughtPiece;
    }
    
    private void UndoMoviment(Position origin, Position target, Piece caughtPiece)
    {
        Piece piece = board.RemovePiece(target);
        piece.SubMove();
        
        if (caughtPiece != null)
        {
            board.PutPiece(caughtPiece, target);
            _capturedPieces.Remove(caughtPiece);
        }
        
        board.PutPiece(piece, origin);
    }

    public void MakeMove(Position origin, Position target)
    {
        Piece caughtPiece = ExecuteMoviment(origin, target);
        
        if (IsInCheck(actualPlayerColor))
        {
            UndoMoviment(origin, target, caughtPiece);
            throw new BoardException("You can't put yourself in check");
        }
        
        if (IsInCheck(Opponent(actualPlayerColor)))
            check = true;
        else
            check = false;
            
        if (IsInCheckmate(Opponent(actualPlayerColor)))
            completed = true;
        else
            completed = false;
        
        turn++; 
        ChangePlayer();
    }

    public void ValideOriginPosition(Position position)
    {
        if (board.piece(position) == null)
            throw new BoardException("Piece not found in that position");
        if (actualPlayerColor != board.piece(position).color)
            throw new BoardException("This piece is not yours");
        if (board.piece(position).HasPossibleMoves() == false)
            throw new BoardException("There are no possible moves for this piece");
    }

    public void ValidadeTargetPosition(Position origin, Position target)
    {
        if (board.piece(origin).CanMoveTo(target) == false)
            throw new BoardException("Invalid target position");
    }

    private void ChangePlayer()
    {
        if (actualPlayerColor == Color.White)
            actualPlayerColor = Color.Black;
        else
            actualPlayerColor = Color.White;
    }

    public void PositionNewPiece(char column, int row, Piece piece)
    {
        board.PutPiece(piece, new ChessPosition(column, row).ToPosition());
        _pieces.Add(piece);
    }
    
    public void SetBoard()  
    {
        PositionNewPiece('c', 1, new Tower(board, Color.White));
        PositionNewPiece('d', 1, new King(board, Color.White));
        PositionNewPiece('e', 1, new Tower(board, Color.White));
        PositionNewPiece('c', 8, new Tower(board, Color.Black));
        PositionNewPiece('d', 8, new King(board, Color.Black));
        PositionNewPiece('e', 8, new Tower(board, Color.Black));
    }

    public HashSet<Piece> GetCapturedPieces(Color color)
    {
        HashSet<Piece> aux = new HashSet<Piece>();

        foreach (Piece piece in _capturedPieces)
        {
            if (piece.GetColor() == color)
            {
                aux.Add(piece);
            }
        }
        
        return aux;
    }
    
    public HashSet<Piece> GetPiecesInGame(Color color)
    {
        HashSet<Piece> aux = new HashSet<Piece>();

        foreach (Piece piece in _pieces)
        {
            if (piece.GetColor() == color)
            {
                aux.Add(piece);
            }
        }

        aux.ExceptWith(GetCapturedPieces(color));
        return aux;
    }

    private Piece IsItKing(Color color)
    {
        foreach (Piece piece in GetPiecesInGame(color))
        {
            if (piece is King)
                return piece;
        }
        
        return null;
    }

    public Color Opponent(Color color)
    {
        if (color == Color.White)
            return Color.Black;
        
        return Color.White;
    }

    public bool IsInCheck(Color color)
    {
        Piece king = IsItKing(color);
        
        if (king == null)
            throw new BoardException("There is no king of this color on the board");

        foreach (var piece in GetPiecesInGame(Opponent(color)))
        {
            bool[,] matrix = piece.PossibleMoves();

            if (matrix[king.position.row, king.position.column])
            {
                return true;
            }
        }
        
        return false;
    }

    public bool IsInCheckmate(Color color)
    {
        if (!IsInCheck(color))
            return false;

        foreach (var piece in GetPiecesInGame(color))
        {
            bool[,] matrix = piece.PossibleMoves();
            
            for (int i = 0; i < board.rows; i++)
            {
                for (int j = 0; j < board.columns; j++)
                {
                    if (matrix[i, j])
                    {
                        Position origin = new Position(piece.position.row, piece.position.column);
                        Position target = new Position(i, j);
                        Piece caughtPiece = ExecuteMoviment(origin, target);
                        bool verifyCheck = IsInCheck(color);
                        UndoMoviment(origin, target, caughtPiece);

                        if (!verifyCheck)
                            return false;
                    }
                }
            }
        }

        return true;
    }
}