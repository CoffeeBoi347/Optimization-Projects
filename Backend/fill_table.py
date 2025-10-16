import mysql.connector as sql
import os

try:

    app = sql.connect(
            host=os.getenv("DB_HOST"),
            user=os.getenv("DB_USER"),
            password=os.getenv("DB_PASSWORD"),
            database=os.getenv("DB_NAME"))

    cursor = app.cursor()

    table_name = "profiles"
    statement = f"INSERT INTO {table_name} (idprofiles, profileName) VALUES (%s, %s)"

    for i in range(0, 100):
        data_username = i + 1
        data_description = f"This is a description for User {i + 1}."

        data_entry = (data_username, data_description)

        cursor.execute(statement, data_entry)

    app.commit()

except:
    print("Elements have already been inserted.")

finally:
    cursor.close()
    app.close()