using System;
using System.Data.SQLite;

class SQLiteHandler
{
    private SQLiteConnection _connection;
    private SQLiteCommand _command;

    public SQLiteHandler(string dbName)
    {
        _connection = new SQLiteConnection($"Data Source={dbName};Version=3;");
        EnsureTablesExist();
        AddDefaultCourses();
        AddDefaultStudents();
    }

    private void Open()
    {
        if (_connection.State != System.Data.ConnectionState.Open)
        {
            _connection.Open();
        }
    }

    private void Close()
    {
        if (_connection.State != System.Data.ConnectionState.Closed)
        {
            _connection.Close();
        }
    }

    private void EnsureTablesExist()
    {
        Open();

        _command = new SQLiteCommand("CREATE TABLE IF NOT EXISTS students (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT, age INT);", _connection);
        _command.ExecuteNonQuery();

        EnsureCourseAndRelationTablesExist();

        Console.WriteLine("Tables ensured to exist.\n");
        Close();
    }

    private void EnsureCourseAndRelationTablesExist()
    {
        Open();

        // Ensure 'courses' table exists
        _command = new SQLiteCommand("CREATE TABLE IF NOT EXISTS courses (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT);", _connection);
        _command.ExecuteNonQuery();

        // Ensure 'student_courses' table exists
        _command = new SQLiteCommand("CREATE TABLE IF NOT EXISTS student_courses (student_id INT, course_id INT, PRIMARY KEY(student_id, course_id), FOREIGN KEY(student_id) REFERENCES students(id), FOREIGN KEY(course_id) REFERENCES courses(id));", _connection);
        _command.ExecuteNonQuery();

        Console.WriteLine("Tables 'courses' and 'student_courses' ensured to exist.\n");
        Close();
    }

    private void AddDefaultCourses()
    {
        Open();

        _command = new SQLiteCommand(@"
            INSERT OR IGNORE INTO courses (id, name) VALUES
            (1, 'Matte'),
            (2, 'Engelska'),
            (3, 'Svenska'),
            (4, 'Programmering');", _connection);
        _command.ExecuteNonQuery();

        Console.WriteLine("Default courses added.\n");
        Close();
    }

    private void AddDefaultStudents()
    {
        Open();

        // Clear the `students` table
        _command = new SQLiteCommand("DELETE FROM students;", _connection);
        _command.ExecuteNonQuery();

        // Reset the ID sequence for the `students` table
        _command = new SQLiteCommand("DELETE FROM sqlite_sequence WHERE name='students';", _connection);
        _command.ExecuteNonQuery();

        // Insert the default records
        _command = new SQLiteCommand(@"
            INSERT INTO students (name, age) VALUES
            ('Alice Johansson', 19),
            ('Bob Karlsson', 20),
            ('Charlie Svensson', 18),
            ('Diana Lind', 22),
            ('Erik Bergström', 21);", _connection);
        _command.ExecuteNonQuery();

        Console.WriteLine("Default students added.\n");
        Close();
    }

    public void AddStudent(string name, int age)
    {
        Open();

        _command = new SQLiteCommand("INSERT INTO students (name, age) VALUES (@name, @age);", _connection);
        _command.Parameters.AddWithValue("@name", name);
        _command.Parameters.AddWithValue("@age", age);

        _command.ExecuteNonQuery();
        Console.WriteLine($"Student '{name}' added.\n");
        Close();
    }

    public void ViewAllStudents()
    {
        Open();

        _command = new SQLiteCommand("SELECT * FROM students;", _connection);
        using var rdr = _command.ExecuteReader();

        Console.WriteLine("All students:");
        while (rdr.Read())
        {
            Console.WriteLine($"ID: {rdr["id"]}, Name: {rdr["name"]}, Age: {rdr["age"]}");
        }
        Console.WriteLine();
        Close();
    }

    public void UpdateStudent(int id, string name, int age)
    {
        Open();

        _command = new SQLiteCommand("UPDATE students SET name = @name, age = @age WHERE id = @id;", _connection);
        _command.Parameters.AddWithValue("@id", id);
        _command.Parameters.AddWithValue("@name", name);
        _command.Parameters.AddWithValue("@age", age);

        _command.ExecuteNonQuery();
        Console.WriteLine($"Student ID {id} updated.\n");
        Close();
    }

    public void DeleteStudent(int id)
    {
        Open();

        _command = new SQLiteCommand("DELETE FROM students WHERE id = @id;", _connection);
        _command.Parameters.AddWithValue("@id", id);

        _command.ExecuteNonQuery();
        Console.WriteLine($"Student ID {id} deleted.\n");
        Close();
    }

    public void AssignCourseToStudent(int studentId, int courseId)
    {
        Open();

        _command.CommandText = "INSERT OR IGNORE INTO student_courses (student_id, course_id) VALUES (@studentId, @courseId);";
        _command.Parameters.Clear();
        _command.Parameters.AddWithValue("@studentId", studentId);
        _command.Parameters.AddWithValue("@courseId", courseId);

        _command.ExecuteNonQuery();
        Console.WriteLine($"Assigned course ID {courseId} to student ID {studentId}.\n");
        Close();
    }

    public void ShowStudentCourses(int studentId)
    {
        Open();

        _command.CommandText = @"
            SELECT c.name FROM courses c
            INNER JOIN student_courses sc ON c.id = sc.course_id
            WHERE sc.student_id = @studentId;";
        _command.Parameters.Clear();
        _command.Parameters.AddWithValue("@studentId", studentId);

        using var rdr = _command.ExecuteReader();
        Console.WriteLine($"Courses for student ID {studentId}:");
        while (rdr.Read())
        {
            Console.WriteLine($"- {rdr.GetString(0)}");
        }
        Console.WriteLine();

        Close();
    }
}

class Program
{
    static void Main()
    {
        SQLiteHandler db = new SQLiteHandler("school.db");

        while (true)
        {
            ShowMenu();

            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    Console.Write("Enter name: ");
                    string name = Console.ReadLine();
                    Console.Write("Enter age: ");
                    if (int.TryParse(Console.ReadLine(), out int age))
                    {
                        db.AddStudent(name, age);
                    }
                    else
                    {
                        Console.WriteLine("Invalid age. Try again.");
                    }
                    break;
                case "2":
                    db.ViewAllStudents();
                    break;
                case "3":
                    Console.Write("Enter student ID to update: ");
                    if (int.TryParse(Console.ReadLine(), out int updateId))
                    {
                        Console.Write("Enter new name: ");
                        string newName = Console.ReadLine();
                        Console.Write("Enter new age: ");
                        if (int.TryParse(Console.ReadLine(), out int newAge))
                        {
                            db.UpdateStudent(updateId, newName, newAge);
                        }
                        else
                        {
                            Console.WriteLine("Invalid age. Try again.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Invalid ID. Try again.");
                    }
                    break;
                case "4":
                    Console.Write("Enter student ID to delete: ");
                    if (int.TryParse(Console.ReadLine(), out int deleteId))
                    {
                        db.DeleteStudent(deleteId);
                    }
                    else
                    {
                        Console.WriteLine("Invalid ID. Try again.");
                    }
                    break;
                case "5":
                    Console.Write("Enter student ID: ");
                    if (int.TryParse(Console.ReadLine(), out int studentId))
                    {
                        Console.Write("Enter course ID (1: Matte, 2: Engelska, 3: Svenska, 4: Programmering): ");
                        if (int.TryParse(Console.ReadLine(), out int courseId))
                        {
                            db.AssignCourseToStudent(studentId, courseId);
                        }
                        else
                        {
                            Console.WriteLine("Invalid course ID. Try again.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Invalid student ID. Try again.");
                    }
                    break;
                case "6":
                    Console.Write("Enter student ID: ");
                    if (int.TryParse(Console.ReadLine(), out int idToView))
                    {
                        db.ShowStudentCourses(idToView);
                    }
                    else
                    {
                        Console.WriteLine("Invalid student ID. Try again.");
                    }
                    break;
                case "0":
                    return;
                default:
                    Console.WriteLine("Invalid choice. Try again.");
                    break;
            }
        }
    }

    static void ShowMenu()
    {
        Console.WriteLine("\n--- Menu ---");
        Console.WriteLine("1. Add a student");
        Console.WriteLine("2. View all students");
        Console.WriteLine("3. Update a student");
        Console.WriteLine("4. Delete a student");
        Console.WriteLine("5. Assign a course to a student");
        Console.WriteLine("6. View student courses");
        Console.WriteLine("0. Exit");
        Console.Write("Select an option: ");
    }
}
