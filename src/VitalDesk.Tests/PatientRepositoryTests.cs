using VitalDesk.Core.Models;
using VitalDesk.Core.Repositories;
using VitalDesk.Core.Migrations;

namespace VitalDesk.Tests;

[TestFixture]
public class PatientRepositoryTests
{
    private IPatientRepository _repository = null!;
    private string _testConnectionString = null!;

    [SetUp]
    public async Task Setup()
    {
        // Use in-memory database for testing
        _testConnectionString = "Data Source=:memory:";
        
        // Initialize test database
        await DatabaseInitializer.InitializeAsync();
        
        _repository = new PatientRepository();
    }

    [Test]
    public async Task CreateAsync_ShouldCreatePatient_WhenValidPatient()
    {
        // Arrange
        var patient = new Patient
        {
            NationalHealthInsurance = "P001",
            Name = "Test Patient",
            Furigana = "テストカンジャ",
            BirthDate = new DateTime(1990, 1, 1),
            Symbol = "SYM001",
            Number = "NUM001",
            InsurerName = "Test Insurance"
        };

        // Act
        var id = await _repository.CreateAsync(patient);

        // Assert
        Assert.That(id, Is.GreaterThan(0));
    }

    [Test]
    public async Task GetByIdAsync_ShouldReturnPatient_WhenPatientExists()
    {
        // Arrange
        var patient = new Patient
        {
            NationalHealthInsurance = "P002",
            Name = "Test Patient 2",
            Furigana = "テストカンジャ2",
            BirthDate = new DateTime(1985, 5, 15)
        };
        var id = await _repository.CreateAsync(patient);

        // Act
        var result = await _repository.GetByIdAsync(id);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.NationalHealthInsurance, Is.EqualTo("P002"));
        Assert.That(result.Name, Is.EqualTo("Test Patient 2"));
    }

    [Test]
    public async Task GetByCodeAsync_ShouldReturnPatient_WhenCodeExists()
    {
        // Arrange
        var patient = new Patient
        {
            NationalHealthInsurance = "P003",
            Name = "Test Patient 3",
            Furigana = "テストカンジャ3",
            BirthDate = new DateTime(1992, 12, 25)
        };
        await _repository.CreateAsync(patient);

        // Act
        var result = await _repository.GetByCodeAsync("P003");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("Test Patient 3"));
    }

    [Test]
    public async Task SearchAsync_ShouldReturnMatchingPatients_WhenSearchTermMatches()
    {
        // Arrange
        var patient1 = new Patient { NationalHealthInsurance = "P004", Name = "John Doe", Furigana = "ジョンドウ" };
        var patient2 = new Patient { NationalHealthInsurance = "P005", Name = "Jane Smith", Furigana = "ジェーンスミス" };
        var patient3 = new Patient { NationalHealthInsurance = "P006", Name = "John Johnson", Furigana = "ジョンジョンソン" };
        
        await _repository.CreateAsync(patient1);
        await _repository.CreateAsync(patient2);
        await _repository.CreateAsync(patient3);

        // Act
        var results = await _repository.SearchAsync("John");

        // Assert
        Assert.That(results.Count(), Is.EqualTo(2));
        Assert.That(results.All(p => p.Name.Contains("John")), Is.True);
    }

    [Test]
    public async Task UpdateAsync_ShouldUpdatePatient_WhenPatientExists()
    {
        // Arrange
        var patient = new Patient
        {
            NationalHealthInsurance = "P007",
            Name = "Original Name",
            Furigana = "オリジナル",
            BirthDate = new DateTime(1980, 1, 1)
        };
        var id = await _repository.CreateAsync(patient);
        
        patient.Id = id;
        patient.Name = "Updated Name";
        patient.Furigana = "アップデート";

        // Act
        var result = await _repository.UpdateAsync(patient);

        // Assert
        Assert.That(result, Is.True);
        
        var updatedPatient = await _repository.GetByIdAsync(id);
        Assert.That(updatedPatient!.Name, Is.EqualTo("Updated Name"));
    }

    [Test]
    public async Task DeleteAsync_ShouldDeletePatient_WhenPatientExists()
    {
        // Arrange
        var patient = new Patient
        {
            NationalHealthInsurance = "P008",
            Name = "To Be Deleted",
            Furigana = "サクジョ"
        };
        var id = await _repository.CreateAsync(patient);

        // Act
        var result = await _repository.DeleteAsync(id);

        // Assert
        Assert.That(result, Is.True);
        
        var deletedPatient = await _repository.GetByIdAsync(id);
        Assert.That(deletedPatient, Is.Null);
    }

    [Test]
    public async Task ExistsAsync_ShouldReturnTrue_WhenPatientCodeExists()
    {
        // Arrange
        var patient = new Patient
        {
            NationalHealthInsurance = "P009",
            Name = "Existing Patient",
            Furigana = "ソンザイ"
        };
        await _repository.CreateAsync(patient);

        // Act
        var exists = await _repository.ExistsAsync("P009");

        // Assert
        Assert.That(exists, Is.True);
    }

    [Test]
    public async Task ExistsAsync_ShouldReturnFalse_WhenPatientCodeDoesNotExist()
    {
        // Act
        var exists = await _repository.ExistsAsync("NONEXISTENT");

        // Assert
        Assert.That(exists, Is.False);
    }
} 