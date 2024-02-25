
using System;
using MathNet.Numerics.LinearAlgebra;
namespace ArtLife;

public class NeuralNetwork
{
    private int inputSize;
    private int hiddenLayer1Size;
    private int hiddenLayer2Size;
    private int outputSize;

    private Matrix<double> weightsInputHidden1;
    private Matrix<double> weightsHidden1Hidden2;
    private Matrix<double> weightsHidden2Output;

    private double[] pastMove;

    public NeuralNetwork(int inputSize, int hiddenLayer1Size, int hiddenLayer2Size, int outputSize)
    {
        this.inputSize = inputSize;
        this.hiddenLayer1Size = hiddenLayer1Size;
        this.hiddenLayer2Size = hiddenLayer2Size;
        this.outputSize = outputSize;
        
        InitializeWeights();
    }

    public double[] GetPastMove()
    {
        if (pastMove != null)
        {
            return pastMove;
        }
        return new double[] {0,0, 0 };;
    }

    private Matrix<double> InitializeWeights(int inputSize, int outputSize)
    {
        double limit = 1.0 / Math.Sqrt(inputSize);
        return Matrix<double>.Build.Random(inputSize, outputSize) * (2.0 * limit) - limit;
    }
    
    private void InitializeWeights()
    {
        Random random = new Random();

// Используйте функцию для инициализации весов для каждого слоя.
        weightsInputHidden1 = InitializeWeights(inputSize, hiddenLayer1Size);
        weightsHidden1Hidden2 = InitializeWeights(hiddenLayer1Size, hiddenLayer2Size);
        weightsHidden2Output = InitializeWeights(hiddenLayer2Size, outputSize);

        pastMove = new double[] {0, 0, 0};
    }
    private double Sigmoid(double x)
    {
        return 1.0 / (1.0 + Math.Exp(-x));
    }
    private double Tanh(double x)
    {
        return Math.Tanh(x);
    }

    public double[] FeedForward(double[] inputs)
    {
        // Преобразовать входные данные в вектор
        var inputVector = Vector<double>.Build.DenseOfArray(inputs);

        // Выполнить операции с матрицами с использованием Math.NET Numerics
        var hidden1Vector = (inputVector.ToRowMatrix() * weightsInputHidden1).Row(0).Map(Tanh);
        var hidden2Vector = (hidden1Vector.ToRowMatrix() * weightsHidden1Hidden2).Row(0).Map(Tanh);
        var outputVector = (hidden2Vector.ToRowMatrix() * weightsHidden2Output).Row(0).Map(Tanh);

        // Преобразовать результат обратно в массив
        pastMove = outputVector.ToArray();
        return outputVector.ToArray();
    }

    public void NewGenom((Matrix<double>, Matrix<double>, Matrix<double>) genom)
    {
        weightsInputHidden1 = genom.Item1;
        weightsHidden1Hidden2 = genom.Item2;
        weightsHidden2Output = genom.Item3;
    }

    public (Matrix<double>, Matrix<double>, Matrix<double>) GetGenom()
    {
        return (weightsInputHidden1, weightsHidden1Hidden2, weightsHidden2Output);
    }
    private Matrix<double> MutateWeights(Matrix<double> weights, double mutationRate, Random random)
    {
        double limit = 1.0 / Math.Sqrt(weights.RowCount);
        Matrix<double> mutatedWeights = weights.Clone();
    
        mutatedWeights.MapInplace(x =>
        {
            double mutation = (random.NextDouble() * 2 - 1) * mutationRate;
            // Убедитесь, что мутация остается в пределах limit.
            double newValue = x + mutation;
            // if (newValue > limit)
            // {
            //     return limit;
            // }
            // else if (newValue < -limit)
            // {
            //     return -limit;
            // }
            return newValue;
        });

        return mutatedWeights;
    }
    public (Matrix<double>, Matrix<double>, Matrix<double>) Mutation()
    {
        Random random = new Random();

        var mutationRate = Config.MUTATION_RATE;

        var newWeightsInputHidden1 = MutateWeights(weightsInputHidden1, mutationRate, random);
        var newWeightsHidden1Hidden2 = MutateWeights(weightsHidden1Hidden2, mutationRate, random);
        var newWeightsHidden2Output = MutateWeights(weightsHidden2Output, mutationRate, random);


        return (newWeightsInputHidden1, newWeightsHidden1Hidden2, newWeightsHidden2Output);
    }
}
