import fastapi
import mysql.connector
import dotenv
import os
import uvicorn

app = fastapi.FastAPI()
dotenv.load_dotenv()

table_name = "profiles"

@app.get("/getElements")
async def getElements(limit: int = 20, offset: int = 0):
    try:
        connect = mysql.connector.connect(
            host=os.getenv("DB_HOST"),
            user=os.getenv("DB_USER"),
            password=os.getenv("DB_PASSWORD"),
            database=os.getenv("DB_NAME")
        )
        cursor = connect.cursor(dictionary=True)

        query = f"SELECT * FROM {table_name} LIMIT %s OFFSET %s"
        cursor.execute(query, (limit, offset))
        data_fetch = cursor.fetchall()
        print(data_fetch)
        return {"count": len(data_fetch), "records": data_fetch}

    except Exception:
        return {"error": str(Exception)}

    finally:
        cursor.close()
        connect.close()


if __name__ == "__main__":
    uvicorn.run("app:app", host="127.0.0.1", port=8000, reload=True)
