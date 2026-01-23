using System;
using System.Collections.Generic;

namespace PlinkoGame.Data
{
    // ============================================
    // INIT DATA MODELS
    // ============================================

    [Serializable]
    public class PlinkoRoot
    {
        public string id;
        public PlinkoGameData gameData;
        public PlinkoPlayer player;
        public PlinkoResultPayload payload;
    }

    [Serializable]
    public class PlinkoGameData
    {
        public List<double> bets;
        public List<RiskLevel> risks;
        public List<PlinkoRow> rows;
    }

    [Serializable]
    public class RiskLevel
    {
        public int id;
        public string name;
        public string description;
    }

    [Serializable]
    public class PlinkoRow
    {
        public string id;
        public List<PlinkoRisk> risks;
    }

    [Serializable]
    public class PlinkoRisk
    {
        public List<double> multipliers;
        public List<double> probability;
    }

    [Serializable]
    public class PlinkoPlayer
    {
        public double balance;
    }

    // ============================================
    // RESULT DATA MODELS
    // ============================================

    [Serializable]
    public class PlinkoResultPayload
    {
        public double winAmount;
        public double multiplier;
        public int generatedMultiplerIndex;
        public int selectedRisk;
        public int selectedRowIndex;
    }

    // ============================================
    // REQUEST DATA MODELS
    // ============================================

    [Serializable]
    public class PlinkoBetRequest
    {
        public string type = "SPIN";
        public PlinkoBetPayload payload = new PlinkoBetPayload();
    }

    [Serializable]
    public class PlinkoBetPayload
    {
        public int betIndex;
        public int selectedRisk;
        public int selectedRowIndex;
    }

    // ============================================
    // INTERNAL GAME STATE
    // ============================================

    [Serializable]
    public class MultiplierMapping
    {
        public int rowCount;
        public string riskLevel;
        public List<double> fullMultipliers;
        public List<int> backendIndices;
    }

    [Serializable]
    public class AuthTokenData
    {
        public string cookie;
        public string socketURL;
        public string nameSpace;
    }
}